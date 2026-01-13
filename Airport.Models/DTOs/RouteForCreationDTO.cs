namespace Airport.Models.DTOs
{
    public class RouteForCreationDTO : RouteForOperationDTO
    {
        private List<DirectionDTO>? _directions;

        public override string RouteName { get; set; } = string.Empty;
        public override List<DirectionDTO> Directions
        {
            get => _directions ?? new List<DirectionDTO>();
            set => _directions = value;
        }
    }
}
