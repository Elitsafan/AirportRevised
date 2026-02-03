using Airport.Models.Entities;
using MongoDB.Bson;

namespace Airport.Contracts.EventArgs.RouteEventArgs
{
    public interface IRouteOperationEventArgs
    {
        ObjectId RouteId { get; }
        string RouteName { get; }
        List<Direction> Directions { get; }
    }
}
