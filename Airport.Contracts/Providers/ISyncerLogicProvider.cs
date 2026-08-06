namespace Airport.Contracts.Providers
{
    public interface ISyncerLogicProvider : IDisposable
    {
        Task<IEnumerable<ISyncerLogic>> GetAllAsync(CancellationToken ct = default);
        Task<ISyncerLogic> GetByIdAsync(ObjectId syncerId, CancellationToken ct = default);
    }
}
