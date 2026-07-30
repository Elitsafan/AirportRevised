namespace Airport.Domain.Repositories
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<T> AddOneAsync(T entity, IClientSessionHandle? session = null, CancellationToken ct = default);
        Task<bool> DeleteOneAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default);
    }
}
