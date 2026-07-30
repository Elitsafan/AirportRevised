using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class RouteDTO
    {
        public ObjectId RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions { get; set; } = new();
    }
}