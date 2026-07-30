namespace Airport.Domain.Factories
{
    public class SyncerLogicFactory : ISyncerLogicFactory
    {
        #region Fields
        private readonly ILogger<SyncerLogic> _logger;
        #endregion

        public SyncerLogicFactory(ILogger<SyncerLogic> logger) => _logger = logger;

        public ISyncerLogicCreator GetCreator(Syncer syncer) => syncer is null
            ? throw new ArgumentNullException(nameof(syncer))
            : (ISyncerLogicCreator)new SyncerLogicCreator(syncer, _logger);
    }
}
