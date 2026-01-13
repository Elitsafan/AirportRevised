namespace Airport.Models.DTOs
{
    public abstract class RouteForOperationDTO
    {
        public abstract string RouteName { get; set; }
        public abstract List<DirectionDTO> Directions { get; set; }
    }
}
