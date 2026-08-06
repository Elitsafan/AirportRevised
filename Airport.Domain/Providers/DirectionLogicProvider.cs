using Airport.Contracts.EventArgs.RouteEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Domain.EventArgs.DirectionEventArgs;
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Providers
{
    // TODO: if IDirectionLogic implements IDisposable, dispose IDirectionLogic on all places
    public class DirectionLogicProvider : IDirectionLogicProvider
    {
        #region Fields
        private readonly IRepositoryManager _repoManager;
        private readonly IMemoryCache _cache;
        private readonly IDomainEvents _domainEvents;
        private readonly IDirectionLogicFactory _directionLogicFactory;
        private readonly ILogger<DirectionLogicProvider> _logger;
        private readonly IConcurrentDictionaryLogic<
            ObjectId,
            List<IDirectionLogic>,
            IDirectionLogic> _routeToDirections;
        private readonly AsyncSemaphore _operationSemaphore;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private const string ALL_DIRECTIONS_KEY = "all_directions";
        private const string ROUTE_DIRECTIONS_PREFIX = "route_directions_";
        #endregion

        public DirectionLogicProvider(
            IRepositoryManager repoManager,
            IDirectionLogicFactory directionLogicFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<DirectionLogicProvider> logger)
        {
            _repoManager = repoManager;
            _directionLogicFactory = directionLogicFactory;
            _cache = cache;
            _domainEvents = domainEvents;
            _logger = logger;
            _operationSemaphore = new(1);
            _routeToDirections = new ConcurrentDictionaryLogic<ObjectId, List<IDirectionLogic>, IDirectionLogic>();

            _domainEvents.StationProviderReset += OnStationProviderResetAsync;
            _domainEvents.StationProviderRefreshed += OnStationProviderRefreshedAsync;

            _domainEvents.RouteCreated += OnRouteCreatedAsync;
            _domainEvents.RouteDeleted += OnRouteDeletedAsync;

            _domainEvents.StationLogicUpdated += OnStationLogicUpdatedAsync;
            _domainEvents.StationsByRouteUpdated += OnStationsByRouteUpdatedAsync;
        }

        public async Task<IEnumerable<IDirectionLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_DIRECTIONS_PREFIX}{routeId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                try
                {
                    var result = (await GetAllAsync(ct))[routeId];

                    entry.Size = result.Count;

                    _logger.LogDebug("Cached {ResultCount} direction logics for route {RouteId}.", result.Count, routeId);

                    return result;
                }
                catch (KeyNotFoundException e)
                {
                    throw new LogicNotFoundException($"Route logic id: {routeId} not found.", e);
                }

            }) ?? Enumerable.Empty<IDirectionLogic>();
        }

        public void Dispose()
        {
            _domainEvents.StationProviderReset -= OnStationProviderResetAsync;
            _domainEvents.StationProviderRefreshed -= OnStationProviderRefreshedAsync;

            _domainEvents.RouteCreated -= OnRouteCreatedAsync;
            _domainEvents.RouteDeleted -= OnRouteDeletedAsync;

            _domainEvents.StationLogicUpdated -= OnStationLogicUpdatedAsync;
            _domainEvents.StationsByRouteUpdated -= OnStationsByRouteUpdatedAsync;

            _operationSemaphore?.Dispose();

            _cache.Dispose();

            _routeToDirections.Dispose();

            GC.SuppressFinalize(this);
        }

        protected async Task<IReadOnlyDictionary<ObjectId, List<IDirectionLogic>>> GetAllAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(ALL_DIRECTIONS_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _routeToDirections.Values.Sum(collection => collection.Count);

                _logger.LogDebug("Cached {Count} stations.", entry.Size);

                return _routeToDirections.ToDictionary().AsReadOnly();

            }) ?? new Dictionary<ObjectId, List<IDirectionLogic>>().AsReadOnly();
        }

        #region Event Handlers
        protected virtual async Task OnStationProviderResetAsync()
        {
            _logger.LogInformation("Resetting direction logics and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Direction logics reset successfully.");

            await _domainEvents.RaiseDirectionLogicProviderResetAsync();
        }

        protected virtual async Task OnStationProviderRefreshedAsync()
        {
            _logger.LogInformation("Refreshing direction logics and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Direction logics refreshed successfully.");

            await _domainEvents.RaiseDirectionLogicProviderRefreshedAsync();
        }

        protected virtual async Task OnRouteCreatedAsync(object? sender, IRouteCreatedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            var newDirectionLogics = (await _repoManager.RouteRepository.GetByIdAsync(args.RouteId))
                .Directions
                .Select(d => _directionLogicFactory.GetCreator(d).Create())
                .ToList();

            if (await _routeToDirections.TryAddAsync(args.RouteId, newDirectionLogics))
            {
                _cache.Remove(ALL_DIRECTIONS_KEY);

                _logger.LogInformation("Direction logics of route id: {RouteId} added to cache.", args.RouteId);
            }
        }

        protected virtual async Task OnRouteDeletedAsync(object? sender, IRouteDeletedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();

            // TODO: Dispose
            if (await _routeToDirections.TryRemoveAsync(args.RouteId))
            {
                _cache.Remove(ALL_DIRECTIONS_KEY);

                _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{args.RouteId}");

                _logger.LogInformation("Direction logics of route Id: {RouteId} removed from cache.", args.RouteId);
            }
        }

        protected virtual async Task OnStationsByRouteUpdatedAsync(object? sender, IStationsByRouteUpdatedEventArgs args)
        {
            await EnsureInitializedAsync();

            using var _ = await _operationSemaphore.EnterAsync();
            // TODO: Dispose
            _routeToDirections.TryGetValue(args.RouteId, out var oldDirectionLogics);

            var updatedDirectionLogics = (await _repoManager.RouteRepository.GetByIdAsync(args.RouteId))
                .Directions
                .Select(d => _directionLogicFactory.GetCreator(d).Create())
                .ToList();

            // TODO: add or update
            if (await _routeToDirections.TryUpdateAsync(args.RouteId, updatedDirectionLogics, oldDirectionLogics!))
            {
                _cache.Remove(ALL_DIRECTIONS_KEY);

                _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{args.RouteId}");

                _logger.LogInformation("Direction logics of route id: {RouteId} updated on cache.", args.RouteId);

                await _domainEvents.RaiseDirectionProviderUpdatedAsync(new DirectionProviderUpdatedEventArgs
                {
                    RouteId = args.RouteId,
                });
            }
        }

        protected virtual async Task OnStationLogicUpdatedAsync(object? sender, IStationLogicUpdatedEventArgs args)
        {
            await EnsureInitializedAsync();

            // If stationId does not exist on any direction
            if (_routeToDirections.Values
                .SelectMany(x => x)
                .All(d => d.From != args.StationId && d.To != args.StationId))
                return;

            using var _ = await _operationSemaphore.EnterAsync();

            _cache.Remove(ALL_DIRECTIONS_KEY);

            foreach (var routeEntry in await _repoManager.RouteRepository.DirectionsOfRoutesContainStationAsync(args.StationId))
            {
                _routeToDirections.TryGetValue(routeEntry.Key, out var oldDirections);

                // Create the new direction logics
                var updatedDirections = routeEntry.Value
                    .Select(d => _directionLogicFactory.GetCreator(d).Create())
                    .ToList();

                // TODO: add or update
                // Update with the new value
                if (await _routeToDirections.TryUpdateAsync(routeEntry.Key, updatedDirections, oldDirections!))
                {
                    // Dispose removed direction logics

                    _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{routeEntry.Key}");
                }
            }
        }
        #endregion

        private async Task EnsureInitializedAsync(CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            using var _ = await _operationSemaphore.EnterAsync(ct);

            if (_isInitialized)
                return;

            await InitializeAsync(ct);

            _isInitialized = true;
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing direction logics from database.");

            await ClearAsync(ct);

            var directionsDic = await _repoManager.RouteRepository.GetAllDirectionsAsync(ct);

            if (directionsDic.Count == 0)
                throw new LogicProvisionFailedException("No routes exist.");

            foreach (var kvp in directionsDic)
            {
                // Creates the direction logics
                var directionLogics = kvp.Value.Select(
                    d => _directionLogicFactory.GetCreator(
                        d).Create())
                    .ToList();

                // Store for searching 
                await _routeToDirections.TryAddAsync(kvp.Key, directionLogics, ct);
            }

            _logger.LogInformation(
                "Successfully initialized {DLsCount} direction logics.",
                _routeToDirections.Values.Sum(dList => dList.Count));
        }

        private async Task ClearAsync(CancellationToken ct = default)
        {
            InvalidateCache();

            await _routeToDirections.ClearAsync(ct);

            _isInitialized = false;
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all direction cache entries");

            _cache.Remove(ALL_DIRECTIONS_KEY);

            foreach (var key in _routeToDirections.Keys)
                _cache.Remove($"{ROUTE_DIRECTIONS_PREFIX}{key}");
        }
    }
}
