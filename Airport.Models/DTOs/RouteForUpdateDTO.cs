namespace Airport.Models.DTOs
{
    public class RouteForUpdateDTO
    {
        private List<DirectionDTO>? _directions;

        public string RouteName { get; set; } = string.Empty;
        public List<DirectionDTO> Directions
        {
            get
            {
                _directions ??= new List<DirectionDTO>();
                return _directions;
            }
            set => _directions = value;
        }
    }
}
