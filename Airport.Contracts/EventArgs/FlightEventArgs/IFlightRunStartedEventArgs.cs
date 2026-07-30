namespace Airport.Contracts.EventArgs.FlightEventArgs
{
    public interface IFlightRunStartedEventArgs
    {
        Flight Flight { get; }
        ObjectId RouteId { get; }
        ObjectId StationId { get; }
    }
}
