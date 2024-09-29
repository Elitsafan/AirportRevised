using Airport.Models.DTOs;
using Airport.Models.Enums;

namespace Airport.Simulator.Abstractions
{
    public interface IFlightGenerator
    {
        /// <summary>
        /// Generates a <see cref="FlightForCreationDTO"></see>/>
        /// </summary>
        /// <param name="flightType">Indicates if the flight is a landing or departure flight</param>
        /// <returns>An <see cref="FlightForCreationDTO"/> instance</returns>
        FlightForCreationDTO GenerateFlight(FlightType flightType);
        /// <summary>
        /// Generates a collection of <see cref="FlightForCreationDTO"></see>/>
        /// </summary>
        /// <returns>The list of the generated flights</returns>
        IEnumerable<FlightForCreationDTO> GenerateFlights(int n = 6);
    }
}
