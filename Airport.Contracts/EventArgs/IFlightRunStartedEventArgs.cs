using Airport.Models.Entities;
using MongoDB.Bson;

namespace Airport.Contracts.EventArgs
{
    public interface IFlightRunStartedEventArgs
    {
        Flight Flight { get; }
        ObjectId RouteId { get; }
    }
}
