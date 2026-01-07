using MongoDB.Bson;
using Airport.Models.Enums;

namespace Airport.Models.DTOs
{
    public class DepartureDTO : FlightDTO
    {
        public override ObjectId FlightId { get; set; }
        public override FlightType FlightType => FlightType.Departure;
    }
}