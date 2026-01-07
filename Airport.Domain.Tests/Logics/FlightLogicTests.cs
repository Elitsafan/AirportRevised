namespace Airport.Domain.Tests.Logics
{
    public class FlightLogicTests : IDisposable
    {
        #region Fields
        private Mock<IRouteLogic> _mockRouteLogic;
        private Mock<IStationLogic> _mockStationLogic;
        private ILogger<FlightLogic> _mockLogger;
        private IFlightLogic _flightLogic;
        private bool _eventFired;
        #endregion

        public FlightLogicTests()
        {
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockLogger = Mock.Of<ILogger<FlightLogic>>();
            _mockStationLogic = new Mock<IStationLogic>();
            _eventFired = false;
            _flightLogic = null!;
        }

        [Fact]
        public void FlightLogic_Created_NotNull()
        {
            _flightLogic = new FlightLogic(new Landing(), _mockRouteLogic.Object, _mockLogger);
            Assert.NotNull(_flightLogic);
        }

        [Fact]
        public async Task RaiseFlightRunDoneAsync_WhenCalled_FlightRunDoneEventIsInvokedAsync()
        {
            var flight = new Landing();
            _eventFired = false;
            _flightLogic = new FlightLogic(flight, _mockRouteLogic.Object, _mockLogger);
            _flightLogic.FlightRunDone += OnFlightRunDoneAsync;
            await _flightLogic.RaiseFlightRunDoneAsync();

            Assert.True(_eventFired);
        }

        [Fact]
        public void RaiseFlightRunDoneAsync_WhenNotCalled_FlightRunDoneEventIsNotInvoked()
        {
            var flight = new Landing();
            var eventFired = false;
            _flightLogic = new FlightLogic(flight, _mockRouteLogic.Object, _mockLogger);
            _flightLogic.FlightRunDone += OnFlightRunDoneAsync;

            Assert.False(eventFired);
        }

        [Fact]
        public async Task RaiseFlightRunStartedAsync_WhenCalled_FlightRunStartedEventIsInvokedAsync()
        {
            var nextLeg = new IStationLogic[] { _mockStationLogic.Object };
            var mockEventArgs = new Mock<IStationOccupiedEventArgs>();
            _mockRouteLogic
                .Setup(x => x.GetNextLeg(null))
                .Returns(nextLeg);
            var flight = new Landing();
            _eventFired = false;
            _flightLogic = new FlightLogic(flight, _mockRouteLogic.Object, _mockLogger);
            mockEventArgs
                .SetupGet(x => x.FlightId)
                .Returns(_flightLogic.FlightId);
            _mockRouteLogic
                .Setup(x => x.EnterLegAsync(
                    _flightLogic,
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => _mockStationLogic
                    .RaiseAsync(x => x.StationOccupiedAsync += null, null!, mockEventArgs.Object)
                    .Wait())
                .ReturnsAsync(_mockStationLogic.Object);
            _flightLogic.FlightRunStarted += OnFlightRunStartedAsync;

            await _flightLogic.RunAsync();
            Assert.True(_eventFired);
        }

        [Fact]
        public void RegisterStationOccupiedDetails_WhenCalled_StationOccupiedRegistered()
        {
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dt = DateTime.Now;
            var actual = _flightLogic.RegisterStationOccupiedDetails(id, dt);

            Assert.True(id == actual.StationId);
            Assert.True(dt == actual.Entrance);
            Assert.Null(actual.Exit);
        }

        [Fact]
        public void RegisterStationClearedDetails_WhenCalled_StationClearedDetailsRegistered()
        {
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dtEntrance = DateTime.Now;
            var dtExit = DateTime.Now.AddSeconds(2);
            _flightLogic.RegisterStationOccupiedDetails(id, dtEntrance);
            var actual = _flightLogic.RegisterStationClearedDetails(id, dtExit);

            Assert.True(id == actual.StationId);
            Assert.True(dtEntrance == actual.Entrance);
            Assert.True(dtExit == actual.Exit);
        }

        [Fact]
        public async Task ThrowIfCancellationRequestedAsync_WhenCalledTwice_ThrowsExceptionAsync()
        {
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            CancellationTokenSource cts = new();
            await _flightLogic.ThrowIfCancellationRequestedAsync(cts);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _flightLogic.ThrowIfCancellationRequestedAsync(cts));
        }

        [Fact]
        public async Task RunAsync_WhenCalled_RunsFlightAsync()
        {
            _mockRouteLogic
                .Setup(x => x.GetNextLeg(null))
                .Returns(() => new IStationLogic[] { _mockStationLogic.Object });
            _mockRouteLogic
                .Setup(x => x.EnterLegAsync(
                    It.IsAny<IFlightLogic>(),
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockStationLogic.Object);
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            var task = _flightLogic.RunAsync();
            await task;
            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task RunAsync_WhenCalled_StartRunAsyncCalledAsync()
        {
            _mockRouteLogic
                .Setup(x => x.GetNextLeg(null))
                .Returns(() => new IStationLogic[] { _mockStationLogic.Object });
            _mockRouteLogic
                .Setup(x => x.EnterLegAsync(
                    It.IsAny<IFlightLogic>(),
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockStationLogic.Object);
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            await _flightLogic.RunAsync(It.IsAny<CancellationToken>());

            _mockRouteLogic
                .Verify(x => x.StartRunAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private async Task OnFlightRunStartedAsync(object? sender, IFlightRunStartedEventArgs args)
        {
            _eventFired = true;
            await Task.CompletedTask;
        }

        private async Task OnFlightRunDoneAsync(object? sender, IFlightRunDoneEventArgs args)
        {
            _eventFired = true;
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_flightLogic is not null)
            {
                _flightLogic.FlightRunStarted -= OnFlightRunStartedAsync;
                _flightLogic.FlightRunDone -= OnFlightRunDoneAsync;
            }
        }
    }
}
