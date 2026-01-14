using Airport.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class DirectionLogicProvider : IDirectionLogicProvider
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<DirectionLogicProvider> _logger;
        private readonly SemaphoreSlim _initializationSemaphore;
        private bool _isInitialized;
        private HashSet<IDirectionLogic> _directionLogics = null!;
        private ConcurrentDictionary<ObjectId, List<IDirectionLogic>> _routeIdToDirectionLogics = null!;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ALL_DIRECTIONS_KEY = "all_direction_logics";
        private const string ROUTE_DIRECTIONS_PREFIX = "route_directions_";
        #endregion

        public DirectionLogicProvider(
            IServiceProvider serviceProvider,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<DirectionLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _initializationSemaphore = new(1, 1);

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;
            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public async Task<IEnumerable<IDirectionLogic>> GetDirectionsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

            var cacheKey = $"{ROUTE_DIRECTIONS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                entry.Size = 1;

                try
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();

                    var route = await scope
                        .ServiceProvider
                        .GetRequiredService<IRepositoryManager>()
                        .RouteRepository
                        .GetRouteByIdAsync(routeId, cancellationToken);

                    var result = route.Directions.Join(
                        _directionLogics,
                        dLeft => new { dLeft.From, dLeft.To },
                        dRight => new { dRight.From, dRight.To },
                        (l, r) => r)
                        .ToList();

                    _logger.LogDebug($"Cached {result.Count} direction logics for route {routeId}");
                    return result;
                }
                catch (ArgumentNullException ex)
                {
                    _logger.LogError(ex, $"Route not found: {routeId}");
                    throw new ArgumentException($"Route not found with ID: {routeId}", ex);
                }
            }) ?? [];
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing direction logics from database");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;
            var directionLogicFactory = scope
                .ServiceProvider
                .GetRequiredService<IDirectionLogicFactory>();

            var allRoutes = await routeRepository.GetAllAsync(cancellationToken);
            _routeIdToDirectionLogics = new ConcurrentDictionary<ObjectId, List<IDirectionLogic>>();
            _directionLogics = new HashSet<IDirectionLogic>();
            foreach (var route in allRoutes)
            {
                // Creates the direction logics
                var directionLogics = route.Directions.Select(
                    d => directionLogicFactory.GetCreator(
                        d).Create());
                // Store for searching 
                _routeIdToDirectionLogics.TryAdd(route.RouteId, new(directionLogics));
                // Store in cache
                foreach (var directionLogic in directionLogics)
                    _directionLogics.Add(directionLogic);
            }

            _logger.LogInformation($"Successfully initialized {_directionLogics.Count} direction logics");
        }

        private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
                return;

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized)
                    return;

                await InitializeAsync(cancellationToken);
                _isInitialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Refreshes all direction logics and clears cache
        /// </summary>
        private async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing direction logics and clearing cache");

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Clear cache first
                InvalidateCache();

                // Clear existing direction logics
                _directionLogics.Clear();
                _routeIdToDirectionLogics.Clear();

                // Re-initialize
                _isInitialized = false;
                await InitializeAsync(cancellationToken);

                _logger.LogInformation("Direction logics refreshed successfully");
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_DIRECTIONS_KEY);
        }

        private async Task OnDataRefreshedAsync() => await RefreshAsync();

        private async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        private async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args) =>
            await RefreshAsync();

        private async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args) =>
            await RefreshAsync();

        private async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args) =>
            await RefreshAsync();
    }
}
