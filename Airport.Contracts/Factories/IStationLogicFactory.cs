using Airport.Contracts.Creators;
using Airport.Models.Entities;

namespace Airport.Contracts.Factories
{
    public interface IStationLogicFactory
    {
        IStationLogicCreator GetCreator(Station station);
    }
}
