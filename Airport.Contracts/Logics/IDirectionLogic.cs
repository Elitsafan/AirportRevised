using MongoDB.Bson;

namespace Airport.Contracts.Logics
{
    public interface IDirectionLogic
    {
        ObjectId From { get; }
        ObjectId To { get; }
    }
}
