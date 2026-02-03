namespace Airport.Contracts.Creators
{
    public interface ILogicCreatorAsync<T>
    {
        Task<T> CreateAsync(CancellationToken ct = default);
    }
}
