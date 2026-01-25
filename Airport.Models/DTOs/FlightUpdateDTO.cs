using Airport.Models.Entities;
using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class FlightUpdateDTO
    {
        public List<OccupationDetails> StationOccupationDetails { get; set; } = new();
        public ObjectId? RouteId { get; set; }
    }
}
