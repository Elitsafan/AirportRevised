using Airport.Contracts.EventArgs.SyncerEventArgs;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Airport.Domain.Providers
{
    public class SyncerLogicProvider : ISyncerLogicProvider
    {
        #region Fields
        private readonly ConcurrentDictionary<ObjectId, ISyncerLogic> _syncers;
        private readonly IRepositoryManager _repoManager;
        private readonly ISyncerLogicFactory _syncerFactory;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<SyncerLogicProvider> _logger;
        private readonly IMemoryCache _cache;
        private readonly AsyncSemaphore _operationSemaphore;
        private bool _isInitialized;

        // Cache configuration
        private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ALL_SYNCERS_KEY = "all_syncers";
        #endregion

        public SyncerLogicProvider(
            IRepositoryManager repoManager,
            ISyncerLogicFactory syncerFactory,
            IMemoryCache cache,
            IDomainEvents domainEvents,
            ILogger<SyncerLogicProvider> logger)
        {
            _repoManager = repoManager;
            _syncerFactory = syncerFactory;
            _domainEvents = domainEvents;
            _cache = cache;
            _syncers = new();
            _operationSemaphore = new(1);
            _logger = logger;

            _domainEvents.SyncersUpdated += OnSyncersUpdatedAsync;
            _domainEvents.SyncersDeleted += OnSyncersDeletedAsync;

            _domainEvents.SectionProviderResetting += OnSectionProviderResettingAsync;
            _domainEvents.SectionProviderRefreshing += OnSectionProviderRefreshingAsync;
        }

        public async Task<IEnumerable<ISyncerLogic>> GetAllAsync(CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            return _cache.GetOrCreate(ALL_SYNCERS_KEY, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;

                entry.Size = _syncers.Count;

                _logger.LogDebug("Cached {Count} syncers.", entry.Size);

                return _syncers.Values;

            }) ?? Enumerable.Empty<ISyncerLogic>();
        }

        public async Task<ISyncerLogic> GetByIdAsync(ObjectId syncerId, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            if (!_syncers.TryGetValue(syncerId, out var result))
                throw new LogicNotFoundException($"Syncer id: {syncerId} not found.");

            return result;
        }

        public void Dispose()
        {
            _cache.Dispose();

            foreach (var entry in _syncers)
                entry.Value.Dispose();

            _syncers.Clear();

            _domainEvents.SyncersUpdated -= OnSyncersUpdatedAsync;
            _domainEvents.SyncersDeleted -= OnSyncersDeletedAsync;

            _domainEvents.SectionProviderResetting -= OnSectionProviderResettingAsync;
            _domainEvents.SectionProviderRefreshing -= OnSectionProviderRefreshingAsync;
        }

        #region Event Handlers
        protected virtual async Task OnSyncersUpdatedAsync(object? sender, ISyncersUpdatedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            foreach (var syncerId in args.SyncerIds)
            {
                try
                {
                    if (_syncers.TryGetValue(syncerId, out var syncerLogic))
                    {
                        var syncer = await _repoManager.SyncerRepository.GetByIdAsync(syncerId);

                        await syncerLogic.UpdateAsync(syncer.Capacity, syncer.SectionCriticalOccupations);

                        _syncers[syncerId] = syncerLogic;

                        _logger.LogInformation("Syncer {SyncerId} cache updated.", syncerId);
                    }
                    else
                    {
                        _syncers[syncerId] = _syncerFactory
                            .GetCreator(await _repoManager.SyncerRepository.GetByIdAsync(syncerId))
                            .Create();

                        _logger.LogInformation("Syncer {SyncerId} added to cache.", syncerId);
                    }
                }
                catch (EntityNotFoundException)
                {
                    throw new LogicNotFoundException($"Syncer id {syncerId} not found.");
                }
            }

            _cache.Remove(ALL_SYNCERS_KEY);
        }

        protected virtual async Task OnSyncersDeletedAsync(object? sender, ISyncersDeletedEventArgs args)
        {
            using var _ = await _operationSemaphore.EnterAsync();

            foreach (var syncerId in args.SyncerIds)
            {
                _syncers[syncerId].Dispose();

                _logger.LogInformation("Syncer {SyncerId} removed from cache.", syncerId);
            }

            _cache.Remove(ALL_SYNCERS_KEY);
        }

        protected virtual async Task OnSectionProviderResettingAsync()
        {
            _logger.LogInformation("Resetting syncer logics and clearing cache");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Syncers reset successfully.");
        }

        protected virtual async Task OnSectionProviderRefreshingAsync()
        {
            _logger.LogInformation("Refreshing syncer logics and clearing cache");

            using var _ = await _operationSemaphore.EnterAsync();

            await InitializeAsync();

            _isInitialized = true;

            _logger.LogInformation("Syncers refreshed successfully.");
        }
        #endregion

        private void Clear()
        {
            InvalidateCache();

            foreach (var entry in _syncers)
                entry.Value.Dispose();

            _syncers.Clear();

            _isInitialized = false;
        }

        private async Task InitializeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Initializing syncer logics from repository");

            Clear();

            foreach (var syncer in await _repoManager.SyncerRepository.GetAllAsync(ct))
                _syncers[syncer.SyncerId] = _syncerFactory.GetCreator(syncer).Create();

            _logger.LogInformation("Successfully initialized {SyncersCount} syncer logics", _syncers.Count);
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

        private void InvalidateCache()
        {
            _logger.LogDebug("Invalidating all syncers cache entries");

            _cache.Remove(ALL_SYNCERS_KEY);
        }
    }
}
