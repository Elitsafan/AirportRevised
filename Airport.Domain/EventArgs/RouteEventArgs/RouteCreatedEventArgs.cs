using Airport.Contracts.EventArgs.RouteEventArgs;

namespace Airport.Domain.EventArgs.RouteEventArgs
{
    public class RouteCreatedEventArgs : IRouteCreatedEventArgs
    {
        private List<Direction>? _directions;

        public ObjectId RouteId { get; init; }
        public required string RouteName { get; init; }
        public required List<Direction> Directions
        {
            get
            {
                _directions ??= new();
                return _directions;
            }
            init => _directions = value;
        }
    }
}
