namespace Airport.Contracts.Factories
{
    public interface IFlightLogicFactory
    {
        Task<IFlightLogicCreator> GetCreatorAsync(Flight flight, CancellationToken ct = default);
    }
}
