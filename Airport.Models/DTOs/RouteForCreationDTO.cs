namespace Airport.Models.DTOs
{
    public class RouteForCreationDTO
    {
        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions { get; set; } = new();
    }
}
