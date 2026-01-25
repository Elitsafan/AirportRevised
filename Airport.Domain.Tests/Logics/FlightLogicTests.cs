namespace Airport.Domain.Tests.Logics
{
    public class FlightLogicTests : IDisposable
    {
        #region Fields
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IStationLogic> _mockStationLogic;
        private readonly ILogger<FlightLogic> _mockLogger;
        private AsyncEventHandler<IFlightRunStartedEventArgs>? _onFlightRunStartedAsync;
        private AsyncEventHandler<IFlightRunDoneEventArgs>? _onFlightRunDoneAsync;
        private IFlightLogic _flightLogic = null!;
        #endregion

        public FlightLogicTests()
        {
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockLogger = Mock.Of<ILogger<FlightLogic>>();
            _mockStationLogic = new Mock<IStationLogic>();
        }

        [Fact]
        public async Task RaiseFlightRunDoneAsync_WhenCalled_FlightRunDoneEventIsInvoked()
        {
            // Arange
            var flight = new Landing();
            var tcs = new TaskCompletionSource<bool>();
            _onFlightRunDoneAsync = (s, e) =>
            {
                tcs.SetResult(true);
                return Task.CompletedTask;
            };
            _flightLogic = new FlightLogic(flight, _mockRouteLogic.Object, _mockLogger);
            _flightLogic.FlightRunDone += _onFlightRunDoneAsync;

            // Act
            await _flightLogic.RaiseFlightRunDoneAsync();

            // Assert
            bool result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(result, "The FlightRunDone event was not raised within the timeout.");
        }

        [Fact]
        public async Task RaiseFlightRunStartedAsync_WhenCalled_FlightRunStartedEventIsInvoked()
        {
            // Arange
            var tcs = new TaskCompletionSource<bool>();
            var nextLeg = new IStationLogic[] { _mockStationLogic.Object };
            var mockEventArgs = new Mock<IStationOccupiedEventArgs>();
            _mockRouteLogic
                .Setup(x => x.GetNextLeg(null))
                .Returns(nextLeg);
            var flight = new Landing();
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
                    .GetAwaiter()
                    .GetResult())
                .ReturnsAsync(_mockStationLogic.Object);
            _onFlightRunStartedAsync = (s, e) =>
            {
                tcs.SetResult(true);
                return Task.CompletedTask;
            };
            _flightLogic.FlightRunStarted += _onFlightRunStartedAsync;

            // Act
            await _flightLogic.RunAsync();

            // Assert
            bool result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(result, "The FlightRunDone event was not raised within the timeout.");
        }

        [Fact]
        public void RegisterStationOccupiedDetails_WhenCalled_StationOccupiedRegistered()
        {
            // Arange
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dt = DateTime.Now;

            // Act
            var actual = _flightLogic.RegisterStationOccupiedDetails(id, dt);

            // Assert
            Assert.True(id == actual.StationId);
            Assert.True(dt == actual.Entrance);
            Assert.Null(actual.Exit);
        }

        [Fact]
        public void RegisterStationClearedDetails_WhenCalled_StationClearedDetailsRegistered()
        {
            // Arange
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dtEntrance = DateTime.Now;
            var dtExit = DateTime.Now.AddSeconds(2);
            _flightLogic.RegisterStationOccupiedDetails(id, dtEntrance);

            // Act
            var actual = _flightLogic.RegisterStationClearedDetails(id, dtExit);

            // Assert
            Assert.True(id == actual.StationId);
            Assert.True(dtEntrance == actual.Entrance);
            Assert.True(dtExit == actual.Exit);
        }

        [Fact]
        public async Task ThrowIfCancellationRequestedAsync_WhenCalledTwice_ThrowsException()
        {
            // Arange
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);
            CancellationTokenSource cts = new();

            // Act
            await _flightLogic.ThrowIfCancellationRequestedAsync(cts);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _flightLogic.ThrowIfCancellationRequestedAsync(cts));
        }

        [Fact]
        public async Task RunAsync_WhenCalled_RunsFlight()
        {
            // Arange
            _mockRouteLogic
                .SetupSequence(x => x.GetNextLeg(null))
                .Returns(() => new IStationLogic[] { _mockStationLogic.Object })
                .Returns(Enumerable.Empty<IStationLogic>); // Anyway the test will stop here
            _mockRouteLogic
                .Setup(x => x.EnterLegAsync(
                    It.IsAny<IFlightLogic>(),
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockStationLogic.Object);
            _flightLogic = new FlightLogic(new Departure(), _mockRouteLogic.Object, _mockLogger);

            // Act
            await _flightLogic.RunAsync();

            // Assert
            _mockRouteLogic.Verify(x => x.StartRunAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockRouteLogic.Verify(
                x => x.EnterLegAsync(
                    _flightLogic,
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            _mockRouteLogic.Verify(x => x.GetNextLeg(It.IsAny<IStationLogic>()), Times.AtLeastOnce);
            _mockStationLogic.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            if (_flightLogic is not null)
            {
                _flightLogic.FlightRunStarted -= _onFlightRunStartedAsync;
                _flightLogic.FlightRunDone -= _onFlightRunDoneAsync;
            }
        }
    }
}
