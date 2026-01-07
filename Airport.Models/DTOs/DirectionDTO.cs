using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class DirectionDTO
    {
        public ObjectId From { get; set; }
        public ObjectId To { get; set; }
    }
}
