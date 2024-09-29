using Airport.Models.DTOs;
using Airport.Models.Enums;
using Airport.Simulator.Abstractions;

namespace Airport.Simulator.Services
{
    internal class FlightGenerator : IFlightGenerator
    {
        public IEnumerable<FlightForCreationDTO> GenerateFlights(int n)
        {
            List<FlightForCreationDTO> flights = new();
            for (int i = 1; i <= n; i++)
                flights.Add(GenerateFlight(i % 2 == 0 ? FlightType.Departure : FlightType.Landing));
            return flights;
        }

        FlightForCreationDTO IFlightGenerator.GenerateFlight(FlightType flightType) => GenerateFlight(flightType);

        // Generates a flight
        private FlightForCreationDTO GenerateFlight(FlightType flightType) => flightType == FlightType.Landing
            ? new LandingForCreationDTO()
            : new DepartureForCreationDTO();
    }
}
