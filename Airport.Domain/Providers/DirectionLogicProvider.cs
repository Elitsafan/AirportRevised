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
        private readonly IDirectionLogicFactory _directionLogicFactory;
        private readonly AsyncSemaphore _initializationSemaphore;
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
            IDirectionLogicFactory directionLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<DirectionLogicProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _directionLogicFactory = directionLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _initializationSemaphore = new(1);

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;
            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public async Task<IEnumerable<IDirectionLogic>> GetDirectionsByRouteIdAsync(
            ObjectId routeId,
            CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_DIRECTIONS_PREFIX}{routeId}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
                await using var scope = _serviceProvider.CreateAsyncScope();

                Route route = await scope
                    .ServiceProvider
                    .GetRequiredService<IRepositoryManager>()
                    .RouteRepository
                    .GetRouteByIdAsync(routeId, ct);

                var result = route.Directions.Join(
                    _directionLogics,
                    dLeft => new { dLeft.From, dLeft.To },
                    dRight => new { dRight.From, dRight.To },
                    (l, r) => r)
                    .ToList();

                entry.Size = result.Count;

                _logger.LogDebug($"Cached {result.Count} direction logics for route {routeId}");
                return result;
            }) ?? [];
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing direction logics from database");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            var allRoutes = await routeRepository.GetAllAsync(ct);
            _routeIdToDirectionLogics = new ConcurrentDictionary<ObjectId, List<IDirectionLogic>>();
            _directionLogics = new HashSet<IDirectionLogic>();
            foreach (var route in allRoutes)
            {
                // Creates the direction logics
                var directionLogics = route.Directions.Select(
                    d => _directionLogicFactory.GetCreator(
                        d).Create());
                // Store for searching 
                _routeIdToDirectionLogics.TryAdd(route.RouteId, new(directionLogics));
                // Store in cache
                foreach (var directionLogic in directionLogics)
                    _directionLogics.Add(directionLogic);
            }

            _logger.LogInformation($"Successfully initialized {_directionLogics.Count} direction logics");
        }

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var releaser = await _initializationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);
            _isInitialized = true;
        }

        /// <summary>
        /// Refreshes all direction logics and clears cache
        /// </summary>
        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing direction logics and clearing cache");

            using var releaser = await _initializationSemaphore.EnterAsync(ct);
            // Clear cache first
            InvalidateCache();

            // Clear existing direction logics
            _directionLogics.Clear();
            _routeIdToDirectionLogics.Clear();

            // Re-initialize
            _isInitialized = false;
            await InitializeAsync(ct);
            _isInitialized = true;

            _logger.LogInformation("Direction logics refreshed successfully");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_DIRECTIONS_KEY);
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync() => await RefreshAsync();

        protected virtual async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args) =>
            await RefreshAsync();

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args) =>
            await RefreshAsync();

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args) =>
            await RefreshAsync(); 
        #endregion
    }
}
