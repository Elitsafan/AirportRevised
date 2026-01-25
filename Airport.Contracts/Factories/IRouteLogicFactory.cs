using Airport.Contracts.Creators;
using Airport.Contracts.Helpers;
using Airport.Models.Entities;

namespace Airport.Contracts.Factories
{
    public interface IRouteLogicFactory
    {
        IRouteLogicCreator GetCreator(Route route, IEnumerable<IRouteSectionDetails>? sections);
    }
}
