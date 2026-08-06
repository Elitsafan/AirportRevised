namespace Airport.Domain.Helpers
{
    public static class FlightExtensions
    {
        public static FlightType ToFlightType(this Flight flight) => flight is null
            ? throw new ArgumentNullException(nameof(flight))
            : flight is Landing
                ? FlightType.Landing
                : FlightType.Departure;
    }
}
