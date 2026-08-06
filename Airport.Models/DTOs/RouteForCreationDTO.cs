namespace Airport.Models.DTOs
{
    public class RouteForCreationDTO
    {
        private List<DirectionDTO>? _directions;

        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions { get; set; } = new();
    }
}
