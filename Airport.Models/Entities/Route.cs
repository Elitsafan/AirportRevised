using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace Airport.Models.Entities
{
    public class Route
    {
        private List<Direction>? _directions;

        [BsonId]
        public ObjectId RouteId { get; set; }

        [BsonRequired]
        [BsonElement("route_name")]
        [JsonProperty("route_name")]
        public string RouteName { get; set; } = string.Empty;

        [BsonElement("directions")]
        [JsonProperty("directions")]
        public List<Direction> Directions
        {
            get => _directions ?? new List<Direction>();
            set => _directions = value;
        }
    }
}
