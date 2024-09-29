namespace Airport.Simulator.Abstractions
{
    public interface IFlightEndPointsConfiguration
    {
        string BaseUrl { get; set; }
        string Departure { get; set; }
        string Landing { get; set; }
        string Start { get; set; }
        string Flights { get; set; }
    }
}