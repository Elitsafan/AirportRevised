namespace Airport.Domain.EventArgs
{
    internal class StationClearedEventArgs : System.EventArgs, IStationClearedEventArgs
    {
        public StationClearedEventArgs(ObjectId routeId, ObjectId flightId, IStationLogic stationLogic)
        {
            StationLogic = stationLogic;
            RouteId = routeId;
            FlightId = flightId;
        }

        public IStationLogic StationLogic { get; }
        public ObjectId RouteId { get; }
        public ObjectId FlightId { get; }
    }
}
