using MongoDB.Bson;
using Airport.Models.Entities;

namespace Airport.Contracts.EventArgs
{
    public interface IFlightRunStartedEventArgs
    {
        Flight Flight { get; }
        ObjectId RouteId { get; }
    }
}
