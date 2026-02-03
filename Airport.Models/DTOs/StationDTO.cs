using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class StationDTO
    {
        public ObjectId StationId { get; set; }
        public FlightDTO? Flight { get; set; }
        public TimeSpan WaitingTime { get; set; }
    }
}