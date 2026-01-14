namespace Airport.Domain.EventArgs
{
    internal class StationOccupiedEventArgs : System.EventArgs, IStationOccupiedEventArgs
    {
        public StationOccupiedEventArgs(ObjectId flightId, IStationLogic stationLogic)
        {
            StationLogic = stationLogic;
            FlightId = flightId;
        }

        public IStationLogic StationLogic { get; }
        public ObjectId FlightId { get; }
    }
}
