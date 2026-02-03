namespace Airport.Models.DTOs
{
    public class RouteForCreationDTO
    {
        private List<DirectionDTO>? _directions;

        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions
        {
            get
            {
                _directions ??= new();
                return _directions;
            }
            set => _directions = value;
        }
    }
}
