namespace Airport.Domain.Repositories
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<T> AddOneAsync(T entity, CancellationToken ct = default);
        Task<bool> DeleteOneAsync(ObjectId id, CancellationToken ct = default);
    }
}
