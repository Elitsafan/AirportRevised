using Airport.Models.DTOs;

namespace Airport.Models
{
    public interface IAirportStatus
    {
        IEnumerable<RouteDTO> Routes { get; init; }
        IEnumerable<StationDTO> Stations { get; init; }
    }
}