using MongoDB.Bson;
using Newtonsoft.Json;

namespace Airport.Models.DTOs
{
    public class StationDTO
    {
        public ObjectId StationId { get; set; }
        public FlightDTO? Flight { get; set; }
        [JsonIgnore]
        public TimeSpan WaitingTime { get; set; }
    }
}