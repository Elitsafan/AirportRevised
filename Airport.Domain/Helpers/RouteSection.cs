namespace Airport.Domain.Helpers
{
    internal class RouteSection : IRouteSection
    {
        #region Fields
        private HashSet<IStationLogic> _source = null!;
        private HashSet<IStationLogic> _destination = null!;
        private readonly HashSet<IStationLogic> _allStations;
        #endregion

        public RouteSection(
            IEnumerable<IStationLogic> source,
            IEnumerable<IStationLogic> destination,
            IEnumerable<IStationLogic> stations,
            ObjectId routeId)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (stations is null)
                throw new ArgumentNullException(nameof(stations));
            if (!source.Any())
                throw new ArgumentException("Collection cannot be empty.", nameof(source));
            if (!destination.Any())
                throw new ArgumentException("Collection cannot be empty.", nameof(destination));
            if (!stations.Any())
                throw new ArgumentException("Collection cannot be empty.", nameof(stations));
            // Keeps an ordered sets for comparing
            _source = new HashSet<IStationLogic>(source.OrderBy(sl => sl.StationId));
            _destination = new HashSet<IStationLogic>(destination.OrderBy(sl => sl.StationId));
            _allStations = new HashSet<IStationLogic>(stations.OrderBy(sl => sl.StationId));
            RouteId = routeId;
        }

        #region Properties
        public ISet<IStationLogic> Source => _source;
        public ISet<IStationLogic> Destination => _destination;

        public ISet<IStationLogic> AllTrafficLights => _source
            .Concat(_destination)
            .OrderBy(sl => sl.StationId)
            .ToHashSet();

        public int AllStationsCount => _allStations.Count;
        public ObjectId RouteId { get; }
        #endregion

        public void AddToSource(IStationLogic stationLogic)
        {
            if (stationLogic is null)
                throw new ArgumentNullException(nameof(stationLogic));
            _source.Add(stationLogic);
            _allStations.Add(stationLogic);
        }
    }
}
