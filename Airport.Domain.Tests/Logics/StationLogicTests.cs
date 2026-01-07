namespace Airport.Domain.Tests.Logics
{
    public class StationLogicTests : IDisposable
    {
        #region Fields
        private Station _station;
        private readonly ILogger<IStationLogic> _mockLogger;
        private Mock<IFlightLogic> _mockFlightLogic;
        private IStationLogic _stationLogic;
        private IStationOccupiedEventArgs _stationOcuupiedArgs;
        private IStationClearedEventArgs _stationClearedArgs;
        #endregion

        public StationLogicTests()
        {
            _mockFlightLogic = new Mock<IFlightLogic>();
            _mockLogger = Mock.Of<ILogger<IStationLogic>>();
            _station = new Station();
            _stationOcuupiedArgs = null!;
            _stationClearedArgs = null!;
            _stationLogic = null!;
        }

        [Fact]
        public void StationLogicCreated_NoFlightSet_CurrentFlightTypeReturnsNull()
        {
            _stationLogic = new StationLogic(_station, _mockLogger);

            Assert.Null(_stationLogic.CurrentFlightType);
        }

        [Fact]
        public void StationLogicCreated_NoFlightSet_CurrentFlightIdReturnsNull()
        {
            _stationLogic = new StationLogic(_station, _mockLogger);

            Assert.Null(_stationLogic.CurrentFlightId);
        }

        [Fact]
        public async Task StationLogicCreated_FlightSet_CurrentFlightTypeReturnsValueAsync()
        {
            var flightType = FlightType.Departure;

            _mockFlightLogic
                .SetupGet(x => x.FlightType)
                .Returns(flightType);

            _stationLogic = new StationLogic(_station, _mockLogger);
            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object);

            Assert.Equal(flightType, _stationLogic.CurrentFlightType);
        }

        [Fact]
        public async Task StationLogicCreated_FlightSet_CurrentFlightIdReturnsValueAsync()
        {
            var flightId = ObjectId.GenerateNewId();

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);

            _stationLogic = new StationLogic(_station, _mockLogger);
            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object);

            Assert.Equal(flightId, _stationLogic.CurrentFlightId);
        }

        [Fact]
        public void StationLogicCreated_StationIdReturnsValue()
        {
            _station.StationId = ObjectId.GenerateNewId();

            _stationLogic = new StationLogic(_station, _mockLogger);

            Assert.Equal(_station.StationId, _stationLogic.StationId);
        }

        [Fact]
        public async Task ClearAsync_NoFlightSet_ThrowsInvalidOperationExceptionAsync()
        {
            _station.StationId = ObjectId.GenerateNewId();

            _stationLogic = new StationLogic(_station, _mockLogger);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _stationLogic.ClearAsync());
        }

        [Fact]
        public async Task SetFlightAsync_NoFlightSet_ThrowsInvalidOperationExceptionAsync()
        {
            _station.StationId = ObjectId.GenerateNewId();

            _stationLogic = new StationLogic(_station, _mockLogger);

            await Assert.ThrowsAsync<ArgumentNullException>(() => _stationLogic.SetFlightAsync(null!));
        }

        [Fact]
        public async Task SetFlightAsync_WhenCalledAfterCancellation_ThrowsOperationCanceledExceptionAsync()
        {
            _station.StationId = ObjectId.GenerateNewId();

            _stationLogic = new StationLogic(_station, _mockLogger);
            var cts = new CancellationTokenSource();
            _mockFlightLogic
                .Setup(x => x.ThrowIfCancellationRequestedAsync(It.IsAny<CancellationTokenSource>()))
                .Callback(cts.Cancel);
            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object, cts);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _stationLogic.SetFlightAsync(new Mock<IFlightLogic>().Object, cts));
            _mockFlightLogic.Verify(x => x.ThrowIfCancellationRequestedAsync(cts));
        }

        [Fact]
        public async Task SetFlightAsync_WhenCalled_RaisesStationOccupiedAsync()
        {
            var flightId = ObjectId.GenerateNewId();

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);

            _station.StationId = ObjectId.GenerateNewId();
            _stationLogic = new StationLogic(_station, _mockLogger);
            _stationLogic.StationOccupiedAsync += OnStationOccupiedAsync;

            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object);

            Assert.Equal(flightId, _stationOcuupiedArgs.FlightId);
        }

        [Fact]
        public async Task ClearAsync_OnFirstStation_NotRaisesStationClearedAsync()
        {
            var flightId = ObjectId.GenerateNewId();
            var routeId = ObjectId.GenerateNewId();

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            _mockFlightLogic
                .SetupGet(x => x.RouteId)
                .Returns(routeId);

            _station.StationId = ObjectId.GenerateNewId();
            _stationLogic = new StationLogic(_station, _mockLogger);
            _stationLogic.StationClearedAsync += OnStationClearedAsync;

            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object);

            Assert.Null(_stationClearedArgs);
        }

        [Fact]
        public async Task SetFlightAsync_WhenCalled_CallsClearedAsync()
        {
            var flightId = ObjectId.GenerateNewId();
            var routeId = ObjectId.GenerateNewId();
            var mockPrevStationLogic = new Mock<IStationLogic>();

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            _mockFlightLogic
                .SetupGet(x => x.RouteId)
                .Returns(routeId);
            _mockFlightLogic
                .SetupGet(x => x.CurrentStation)
                .Returns(mockPrevStationLogic.Object);
            mockPrevStationLogic
                .Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _station.StationId = ObjectId.GenerateNewId();
            _stationLogic = new StationLogic(_station, _mockLogger);

            await _stationLogic.SetFlightAsync(_mockFlightLogic.Object);

            mockPrevStationLogic.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task OnStationOccupiedAsync(object? sender, IStationOccupiedEventArgs args)
        {
            _stationOcuupiedArgs = args;
            await Task.CompletedTask;
        }

        private async Task OnStationClearedAsync(object? sender, IStationClearedEventArgs args)
        {
            _stationClearedArgs = args;
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_stationLogic is not null)
            {
                _stationLogic.StationOccupiedAsync -= OnStationOccupiedAsync;
                _stationLogic.StationClearedAsync -= OnStationClearedAsync;
            }
        }
    }
}
