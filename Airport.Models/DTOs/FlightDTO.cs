using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public abstract class FlightDTO
    {
        public abstract FlightType FlightType { get; }
        public abstract ObjectId FlightId { get; set; }
    }
}
