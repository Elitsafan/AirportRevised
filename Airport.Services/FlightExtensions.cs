using Airport.Models.Entities;
using Airport.Models.Enums;

namespace Airport.Services
{
    internal static class FlightExtensions
    {
        public static FlightType ConvertToFlightType(this Flight flight) => flight is Departure
            ? FlightType.Departure
            : FlightType.Landing;
    }
}
