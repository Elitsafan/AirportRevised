namespace Airport.Simulator.Abstractions
{
    public interface IAuthService : IDisposable
    {
        Task<string?> LoginAsync(CancellationToken ct = default);
    }
}
