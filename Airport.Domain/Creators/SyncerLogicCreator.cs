namespace Airport.Domain.Creators
{
    public class SyncerLogicCreator : ISyncerLogicCreator
    {
        #region Fields
        private readonly Syncer _syncer;
        private readonly ILogger<SyncerLogic> _logger;
        #endregion

        public SyncerLogicCreator(Syncer syncer, ILogger<SyncerLogic> logger)
        {
            _syncer = syncer;
            _logger = logger;
        }

        public ISyncerLogic Create() => new SyncerLogic(_syncer, _logger);
    }
}
