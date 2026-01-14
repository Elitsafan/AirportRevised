namespace Airport.Domain.EventArgs
{
    internal class StationClearingEventArgs : System.EventArgs, IStationClearingEventArgs
    {
        public StationClearingEventArgs(ObjectId flightId, IStationLogic stationLogic)
        {
            StationLogic = stationLogic;
            FlightId = flightId;
        }

        public IStationLogic StationLogic { get; }
        public ObjectId FlightId { get; }
    }
}
