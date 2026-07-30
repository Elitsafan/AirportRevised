using Airport.Contracts.EventArgs.SectionEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Microsoft.Extensions.Caching.Memory;

namespace Airport.Domain.Providers
{
    public class SectionLogicProvider : ISectionLogicProvider
    {
        #region Fields
        private readonly IRepositoryManager _repoManager;
        private readonly ISectionLogicFactory _sectionLogicFactory;
        private readonly IDomainEvents _domainEvents;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SectionLogicProvider> _logger;
        private readonly AsyncSemaphore _operationSemaphore;
        private readonly IConcurrentDictionaryLogic<
            ObjectId,
            List<ISectionLogic>,
            ISectionLogic> _routeToSections;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private const string ALL_SECTIONS_KEY = "all_sections";
        private const string ROUTE_SECTIONS_PREFIX = "route_sections_";
        #endregion

        public SectionLogicProvider(
            IRepositoryManager repoManager,
            ISectionLogicFactory sectionLogicFactory,
            IDomainEvents domainEvents,
            IMemoryCache cache,
            ILogger<SectionLogicProvider> logger)
        {
            _repoManager = repoManager;
            _sectionLogicFactory = sectionLogicFactory;
            _domainEvents = domainEvents;
            _cache = cache;
            _logger = logger;

            _operationSemaphore = new(1);

            _routeToSections = new ConcurrentDictionaryLogic<
                ObjectId,
                List<ISectionLogic>,
                ISectionLogic>();

            _domainEvents.SectionsCreated += OnSectionsCreatedAsync;
            _domainEvents.SectionsDeleted += OnSectionsDeletedAsync;

            _domainEvents.StationLogicUpdated += OnStationLogicUpdatedAsync;

            _domainEvents.DirectionProviderRefreshed += OnDirectionProviderRefreshedAsync;
            _domainEvents.DirectionProviderReset += OnDirectionProviderResetAsync;
        }

        public async Task<IReadOnlyDictionary<ObjectId, List<ISectionLogic>>> GetAllAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(ALL_SECTIONS_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _routeToSections.Values.Sum(sections => sections.Count);

                _logger.LogDebug("Cached {Count} sections of all routes", entry.Size);

                return _routeToSections.ToDictionary();

            }) ?? new();
        }

        public async Task<IEnumerable<ISectionLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var cacheKey = $"{ROUTE_SECTIONS_PREFIX}{routeId}";

            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                var result = _routeToSections.GetValue(routeId);

                entry.Size = result.Count;

                _logger.LogDebug("Cached {Count} sections of route id: {id}", result.Count, routeId);

                return result;

            }) ?? Enumerable.Empty<ISectionLogic>();
        }

        public void Dispose()
        {
            _operationSemaphore?.Dispose();
            _cache.Dispose();

            _domainEvents.SectionsCreated -= OnSectionsCreatedAsync;
            _domainEvents.SectionsDeleted -= OnSectionsDeletedAsync;

            _domainEvents.StationLogicUpdated -= OnStationLogicUpdatedAsync;

            _domainEvents.DirectionProviderRefreshed -= OnDirectionProviderRefreshedAsync;
            _domainEvents.DirectionProviderReset -= OnDirectionProviderResetAsync;

            _routeToSections.Dispose();

            GC.SuppressFinalize(this);
        }

        #region Event Handlers
        protected virtual async Task OnDirectionProviderResetAsync()
        {
            _logger.LogInformation("Resetting sections and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await _domainEvents.RaiseSectionProviderResettingAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Sections reset successfully.");

            await _domainEvents.RaiseSectionProviderResetAsync();
        }

        protected virtual async Task OnDirectionProviderRefreshedAsync()
        {
            _logger.LogInformation("Refreshing sections and clearing cache.");

            using var _ = await _operationSemaphore.EnterAsync();

            await _domainEvents.RaiseSectionProviderRefreshingAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Sections refreshed successfully.");

            await _domainEvents.RaiseSectionLogicProviderRefreshedAsync();
        }

        protected virtual async Task OnSectionsCreatedAsync(object? sender, ISectionsCreatedEventArgs args)
        {
            if (args.SectionIds is null)
                return;

            using var _ = await _operationSemaphore.EnterAsync();

            // Get the sections, and filter the new sections only
            var newSections = (await _repoManager.SectionRepository.GetByRouteIdAsync(args.RouteId))
                .IntersectBy(
                    args.SectionIds!,
                    s => s.SectionId)
                .ToList();

            // Create new section logics
            var newSectionLogics = (await Task.WhenAll(
                newSections.Select(
                    async section => await _sectionLogicFactory.GetCreator(
                        section).CreateAsync())))
                .ToList();

            await _routeToSections.AddOrUpdateAsync(
                args.RouteId,
                newSectionLogics,
                (routeId, oldValue) =>
                {
                    oldValue.AddRange(newSectionLogics);
                    return Task.FromResult(oldValue);
                });

            _cache.Remove(ALL_SECTIONS_KEY);

            _cache.Remove($"{ROUTE_SECTIONS_PREFIX}{args.RouteId}");

            _logger.LogInformation("new section logics created:\n{SectionId}.", string.Join("\n", args.SectionIds!));
        }

        protected virtual async Task OnStationLogicUpdatedAsync(object? sender, IStationLogicUpdatedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            _cache.Remove(ALL_SECTIONS_KEY);

            foreach (var sectionEntry in await _repoManager.SectionRepository.SectionsContainAsync(args.StationId))
            {
                // Get updated route sections and
                // create new section logic for each of them
                var updatedSections = (await Task.WhenAll(sectionEntry.Value
                    .Select(async s => await _sectionLogicFactory.GetCreator(s).CreateAsync())))
                    .ToList();

                var oldSections = _routeToSections.GetValue(sectionEntry.Key).ToList();

                if (await _routeToSections.TryUpdateAsync(sectionEntry.Key, updatedSections, oldSections))
                {
                    oldSections.ForEach(s =>
                    {
                        s.Dispose();

                        _logger.LogInformation("Section id {SectionId} updated on cache.", s.SectionId);
                    });

                    _cache.Remove($"{ROUTE_SECTIONS_PREFIX}{sectionEntry.Key}");
                }
            }
        }

        protected virtual async Task OnSectionsDeletedAsync(object? sender, ISectionsDeletedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            var oldSections = _routeToSections.GetValue(args.RouteId);

            List<ISectionLogic> remainingSections = new();

            remainingSections = oldSections
                .ExceptBy(args.SectionIds ?? Enumerable.Empty<ObjectId>(), s => s.SectionId)
                .ToList();

            if (await _routeToSections.TryUpdateAsync(args.RouteId, remainingSections, oldSections.ToList()))
            {
                _cache.Remove(ALL_SECTIONS_KEY);

                _cache.Remove($"{ROUTE_SECTIONS_PREFIX}{args.RouteId}");

                _logger.LogInformation(
                    "Old section logics deleted:\n{OldSections}.",
                    string.Join("\n", oldSections.Select(s => s.SectionId)));

                foreach (var section in oldSections)
                    section.Dispose();
            }
        }
        #endregion

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing section logics from repository");

            await ClearAsync(ct);

            var allSectionsDic = await _repoManager.SectionRepository.AllSectionsByRouteIdsAsync(ct);

            foreach (var sectionEntry in allSectionsDic)
            {
                // Create section logics of each route
                var sectionLogics = (await Task.WhenAll(sectionEntry.Value
                    .Select(async s => await _sectionLogicFactory.GetCreator(s).CreateAsync())))
                    .ToList();

                await _routeToSections.TryAddAsync(sectionEntry.Key, sectionLogics, ct);
            }

            _logger.LogInformation("Successfully initialized {SectionsCount} section logics", allSectionsDic.Values.Sum(sl => sl.Count));
        }

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

        private async Task ClearAsync(CancellationToken ct = default)
        {
            InvalidateCache();

            await _routeToSections.ClearAsync(ct);

            _isInitialized = false;
        }

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all section cache entries");

            _cache.Remove(ALL_SECTIONS_KEY);

            foreach (var key in _routeToSections.Keys)
                _cache.Remove($"{ROUTE_SECTIONS_PREFIX}{key}");
        }
    }
}
