using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
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
        private readonly IDirectionLogicFactory _directionLogicFactory;
        private readonly ILogger<DirectionLogicProvider> _logger;
        private readonly HashSet<IDirectionLogic> _directionLogics;
        private readonly ConcurrentDictionary<ObjectId, List<IDirectionLogic>> _routeIdToDirectionLogics;
        private readonly AsyncSemaphore _initializationSemaphore;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
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
            _directionLogics = new();
            _routeIdToDirectionLogics = new();

            _domainEvents.RouteCreated += OnRouteCreatedAsync;
            _domainEvents.RouteUpdated += OnRouteUpdatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationCreated += OnStationCreatedAsync;
            _domainEvents.StationDeleted += OnStationDeletedAsync;
            _domainEvents.StationUpdated += OnStationUpdatedAsync;

            _domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _domainEvents.SystemResetRequested += OnSystemResetRequestedAsync;
        }

        public async Task<IEnumerable<IDirectionLogic>> GetByRouteIdAsync(
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
                    .GetByIdAsync(routeId, ct);

                var result = route.Directions.Join(
                    _directionLogics,
                    dLeft => new { dLeft.From, dLeft.To },
                    dRight => new { dRight.From, dRight.To },
                    (l, r) => r)
                    .ToList();

                entry.Size = result.Count;

                _logger.LogDebug($"Cached {result.Count} direction logics for route {routeId}.");
                return result;
            }) ?? Enumerable.Empty<IDirectionLogic>();
        }

        public void Dispose()
        {
            _initializationSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }

        #region Event Handlers
        protected virtual async Task OnDataRefreshedAsync() => await RefreshAsync();

        protected virtual async Task OnSystemResetRequestedAsync() => await RefreshAsync();

        protected virtual async Task OnRouteCreatedAsync(object? sender, IRouteCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            _routeIdToDirectionLogics[args.RouteId] = new();

            foreach (var direction in args.Directions)
            {
                var newDirection = _directionLogicFactory
                    .GetCreator(direction)
                    .Create();
                _directionLogics.Add(newDirection);
                _routeIdToDirectionLogics[args.RouteId].Add(newDirection);
            }

            InvalidateCache();

            _logger.LogInformation($"Direction logics of route Id: {args.RouteId} added to cache.");
        }

        protected virtual async Task OnRouteUpdatedAsync(object? sender, IRouteUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            _directionLogics.RemoveWhere(
                dl => args.OldRoute.Directions.Any(
                    d => dl.From == d.From && dl.To == d.To));

            _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{args.RouteId}");

            _routeIdToDirectionLogics[args.RouteId] = new();

            foreach (var direction in args.Directions)
            {
                var newDirection = _directionLogicFactory
                    .GetCreator(direction)
                    .Create();
                _directionLogics.Add(newDirection);
                _routeIdToDirectionLogics[args.RouteId].Add(newDirection);
            }

            InvalidateCache();

            _logger.LogInformation($"Direction logics of route Id: {args.RouteId} updated on cache.");
        }

        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            _directionLogics.RemoveWhere(
                dl => args.Directions.Any(
                    d => dl.From == d.From && dl.To == d.To));
            _routeIdToDirectionLogics.TryRemove(args.RouteId, out var directionLogics);
            _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{args.RouteId}");

            InvalidateCache();

            _logger.LogInformation($"Direction logics of route Id: {args.RouteId} removed from cache.");
        }

        protected virtual async Task OnStationCreatedAsync(object? sender, IStationCreatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();
            InvalidateCache();
        }

        protected virtual async Task OnStationUpdatedAsync(object? sender, IStationUpdatedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            foreach (var routeEntry in _routeIdToDirectionLogics)
                if (routeEntry.Value.Any(
                    dl => dl.From == args.StationId || dl.To == args.StationId))
                    _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{routeEntry.Key}");
            InvalidateCache();
        }

        protected virtual async Task OnStationDeletedAsync(object? sender, IStationDeletedEventArgs args)
        {
            using var _ = await _initializationSemaphore.EnterAsync();

            _directionLogics.RemoveWhere(dl => dl.From == args.StationId || dl.To == args.StationId);
            foreach (var routeEntry in _routeIdToDirectionLogics)
                if (routeEntry.Value.Any(
                    dl => dl.From == args.StationId || dl.To == args.StationId))
                {
                    routeEntry.Value.RemoveAll(
                        dl => dl.From == args.StationId || dl.To == args.StationId);
                    _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{routeEntry.Key}");
                }
            InvalidateCache();
        }
        #endregion

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing direction logics from database.");

            await using var scope = _serviceProvider.CreateAsyncScope();
            var routeRepository = scope
                .ServiceProvider
                .GetRequiredService<IRepositoryManager>()
                .RouteRepository;

            foreach (var route in await routeRepository.GetAllAsync(ct))
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
            _logger.LogInformation($"Successfully initialized {_directionLogics.Count} direction logics.");
        }

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var _ = await _initializationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);
            _isInitialized = true;
        }

        private async Task RefreshAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Refreshing direction logics and clearing cache.");

            using var _ = await _initializationSemaphore.EnterAsync(ct);

            InvalidateCache();

            _directionLogics.Clear();
            _routeIdToDirectionLogics.Clear();

            // Re-initialize
            _isInitialized = false;
            await InitializeAsync(ct);
            _isInitialized = true;

            _logger.LogInformation("Direction logics refreshed successfully.");
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all cache entries");

            // Remove the main cache entry
            _cache.Remove(ALL_DIRECTIONS_KEY);
        }
    }
}
