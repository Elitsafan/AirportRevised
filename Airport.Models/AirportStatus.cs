using Airport.Models.DTOs;

namespace Airport.Models
{
    public class AirportStatus : IAirportStatus
    {
        public IEnumerable<StationDTO> Stations { get; init; } = Enumerable.Empty<StationDTO>();
        public IEnumerable<RouteDTO> Routes { get; init; } = Enumerable.Empty<RouteDTO>();
    }
}
