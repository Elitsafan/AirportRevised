using Airport.Contracts.EventArgs.FlightEventArgs;

namespace Airport.Domain.Tests.Logics
{
    public class FlightLogicTests
    {
        #region Fields
        private readonly Mock<IRouteLogic> _mockRouteLogic;
        private readonly Mock<IStationLogic> _mockStationLogic;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<FlightLogic> _mockLogger;
        private IFlightLogic _sut = null!;
        #endregion

        public FlightLogicTests()
        {
            _mockRouteLogic = new Mock<IRouteLogic>();
            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockLogger = Mock.Of<ILogger<FlightLogic>>();
            _mockStationLogic = new Mock<IStationLogic>();
        }

        [Fact]
        public async Task RaiseFlightRunDoneAsync_WhenCalled_FlightRunDoneEventIsInvoked()
        {
            // Arange
            _sut = new FlightLogic(
                new Landing(),
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            await _sut.RaiseFlightRunDoneAsync();

            // Assert
            _mockDomainEvents.Verify(
                x => x.RaiseFlightRunDoneAsync(It.IsAny<IFlightRunDoneEventArgs>()),
                Times.Once,
                "FlightLogic failed to trigger RaiseFlightRunDoneAsync on IDomainEvents.");
        }

        [Fact]
        public async Task RaiseFlightRunStartedAsync_WhenCalled_FlightRunStartedEventIsInvoked()
        {
            // Arrange
            var flight = new Landing();
            var stationId = new ObjectId();

            _sut = new FlightLogic(
                flight,
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            await _sut.RaiseFlightRunStartedAsync(stationId);

            // Assert
            _mockDomainEvents.Verify(
                x => x.RaiseFlightRunStartedAsync(It.IsAny<IFlightRunStartedEventArgs>()),
                Times.Once,
                "FlightLogic failed to trigger RaiseFlightRunStartedAsync on IDomainEvents.");
        }

        [Fact]
        public void RegisterStationOccupiedDetails_WhenCalled_StationOccupiedRegistered()
        {
            // Arange
            _sut = new FlightLogic(
                new Departure(),
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dt = DateTime.Now;

            // Act
            var actual = _sut.RegisterStationOccupiedDetails(id, dt);

            // Assert
            Assert.True(id == actual.StationId);
            Assert.True(dt == actual.Entrance);
            Assert.Null(actual.Exit);
        }

        [Fact]
        public void RegisterStationClearedDetails_WhenCalled_StationClearedDetailsRegistered()
        {
            // Arange
            _sut = new FlightLogic(
                new Departure(),
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);
            var id = ObjectId.GenerateNewId();
            var dtEntrance = DateTime.Now;
            var dtExit = DateTime.Now.AddSeconds(2);
            _sut.RegisterStationOccupiedDetails(id, dtEntrance);

            // Act
            var actual = _sut.RegisterStationClearedDetails(id, dtExit);

            // Assert
            Assert.True(id == actual.StationId);
            Assert.True(dtEntrance == actual.Entrance);
            Assert.True(dtExit == actual.Exit);
        }

        [Fact]
        public async Task ThrowIfCancellationRequestedAsync_WhenCalledTwice_ThrowsException()
        {
            // Arange
            _sut = new FlightLogic(
                new Departure(),
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);

            CancellationTokenSource cts = new();

            // Act
            await _sut.ThrowIfCancellationRequestedAsync(cts);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _sut.ThrowIfCancellationRequestedAsync(cts));
        }

        [Fact]
        public async Task RunAsync_WhenCalled_RunsFlight()
        {
            // Arange
            _mockRouteLogic
                .SetupSequence(x => x.GetNextLeg(null))
                .Returns(() => new[] { _mockStationLogic.Object })
                .Returns(Enumerable.Empty<IStationLogic>); // Anyway the test will stop here
            _mockRouteLogic
                .Setup(x => x.EnterLegAsync(
                    It.IsAny<IFlightLogic>(),
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_mockStationLogic.Object);

            _sut = new FlightLogic(
                new Departure(),
                _mockRouteLogic.Object,
                _mockDomainEvents.Object,
                _mockLogger);

            // Act
            await _sut.RunAsync();

            // Assert
            _mockRouteLogic.Verify(x => x.StartRunAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockRouteLogic.Verify(
                x => x.EnterLegAsync(
                    _sut,
                    It.IsAny<IEnumerable<IStationLogic>>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            _mockRouteLogic.Verify(x => x.GetNextLeg(It.IsAny<IStationLogic>()), Times.AtLeastOnce);
            _mockStationLogic.Verify(x => x.ClearAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
