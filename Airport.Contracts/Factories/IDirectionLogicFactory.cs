using Airport.Contracts.Creators;
using Airport.Models.Entities;

namespace Airport.Contracts.Factories
{
    public interface IDirectionLogicFactory
    {
        IDirectionLogicCreator GetCreator(Direction direction);
    }
}
