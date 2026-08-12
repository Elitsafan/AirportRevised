using Airport.Models.Entities;
using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class FlightForUpdateDTO
    {
        public ObjectId? RouteId { get; set; }
        public List<OccupationDetails> StationOccupationDetails { get; set; } = new();
    }
}
