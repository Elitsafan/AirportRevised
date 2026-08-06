using Airport.Models.DTOs;

namespace Airport.Models
{
    public class AirportStatus : IAirportStatus
    {
        public required IEnumerable<StationDTO> Stations { get; init; }
        public required IEnumerable<RouteDTO> Routes { get; init; }
    }
}
