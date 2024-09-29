using MongoDB.Bson;

namespace Airport.Models.DTOs
{
    public class RouteDTO
    {
        private List<DirectionDTO>? _directions;

        public ObjectId RouteId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions
        {
            get => _directions ?? new List<DirectionDTO>();
            set => _directions = value;
        }
    }
}