namespace Airport.Contracts.Providers
{
    public interface ISectionLogicProvider : IDisposable
    {
        Task<IReadOnlyDictionary<ObjectId, List<ISectionLogic>>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<ISectionLogic>> GetByRouteIdAsync(ObjectId routeId, CancellationToken ct = default);
    }
}
