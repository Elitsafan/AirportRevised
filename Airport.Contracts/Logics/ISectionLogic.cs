namespace Airport.Contracts.Logics
{
    public interface ISectionLogic : IDisposable
    {
        ObjectId SectionId { get; }
        /// <summary>
        /// Represents the route id associates with the route section
        /// </summary>
        ObjectId RouteId { get; }
        /// <summary>
        /// The set of stations used to enter the route section
        /// </summary>
        List<IStationLogic> Origin { get; }
        /// <summary>
        /// The set of stations used to exit the route section
        /// </summary>
        List<IStationLogic> Destination { get; }
        /// <summary>
        /// Represent the stations logics that aren't traffic lights and belong to the section
        /// </summary>
        List<IStationLogic> SectionOnly { get; }
        /// <summary>
        /// Represent all the traffic lights of the route section
        /// </summary>
        HashSet<IStationLogic> TrafficLights { get; }

        /// <summary>
        /// Enters a flight to a station
        /// </summary>
        /// <param name="station"></param>
        /// <param name="flightId"></param>
        /// <param name="trafficLightsCts"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task EnterSectionAsync(
            IStationLogic station,
            ObjectId flightId,
            CancellationTokenSource? trafficLightsCts,
            CancellationToken ct = default);
    }
}
