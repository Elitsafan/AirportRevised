using MongoDB.Bson;
using Airport.Contracts.Logics;

namespace Airport.Contracts.Helpers
{
    public interface IRouteSection
    {
        /// <summary>
        /// The set of stations used to enter the route section
        /// </summary>
        ISet<IStationLogic> Source { get; }
        /// <summary>
        /// The set of stations used to exit the route section
        /// </summary>
        ISet<IStationLogic> Destination { get; }
        /// <summary>
        /// Represent all the traffic lights of the route section
        /// </summary>
        ISet<IStationLogic> AllTrafficLights { get; }
        /// <summary>
        /// Represents the route id associates with the route section
        /// </summary>
        ObjectId RouteId { get; }
        /// <summary>
        /// The number of all stations of the route section
        /// </summary>
        int AllStationsCount { get; }
        /// <summary>
        /// Adds the <paramref name="stationLogic"/> to <see cref="IRouteSection.Source"/>
        /// and updates <see cref="IRouteSection.AllStationsCount"/>
        /// </summary>
        /// <param name="stationLogic"></param>
        void AddToSource(IStationLogic stationLogic);
    }
}
