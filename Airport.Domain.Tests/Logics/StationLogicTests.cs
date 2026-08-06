using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Models.Entities;

namespace Airport.Domain.Tests.Logics
{
    public class StationLogicTests : IDisposable
    {
        #region Fields
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly ILogger<StationLogic> _mockLogger;
        private IStationLogic _sut = null!;
        //private AsyncEventHandler<IStationOccupiedEventArgs>? _onStationOccupiedAsync;
        //private AsyncEventHandler<IStationClearingEventArgs>? _onStationClearingAsync;
        private AsyncEventHandler<IStationClearedEventArgs>? _onStationClearedAsync;
        #endregion

        public StationLogicTests()
        {
            _mockLogger = Mock.Of<ILogger<StationLogic>>();
            _mockDomainEvents = new Mock<IDomainEvents>();
        }

        [Fact]
        public void StationLogicCreated_NoFlightSet_CurrentFlightTypeReturnsNull()
        {
            // Arrange
            var station = new Station();
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act & Assert
            Assert.Null(_sut.CurrentFlightType);
        }

        [Fact]
        public void StationLogicCreated_NoFlightSet_CurrentFlightIdReturnsNull()
        {
            // Arrange
            var station = new Station();
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);
            var currentFlightId = _sut.CurrentFlightId;

            // Act & Assert
            Assert.Null(_sut.CurrentFlightId);
        }

        [Fact]
        public async Task StationLogicCreated_FlightSet_CurrentFlightTypeReturnsCorrectValue()
        {
            // Arrange
            var station = new Station();
            var flightType = FlightType.Departure;
            var mockFlightLogic = new Mock<IFlightLogic>();
            mockFlightLogic
                .SetupGet(x => x.FlightType)
                .Returns(flightType);
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act
            await _sut.SetFlightAsync(mockFlightLogic.Object, null);

            // Assert
            Assert.Equal(mockFlightLogic.Object.FlightType, _sut.CurrentFlightType);
        }

        [Fact]
        public async Task StationLogicCreated_FlightSet_CurrentFlightIdReturnsCorrectValue()
        {
            // Arrange
            var station = new Station();
            var flightId = ObjectId.GenerateNewId();
            var mockFlightLogic = new Mock<IFlightLogic>();
            mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act
            await _sut.SetFlightAsync(mockFlightLogic.Object, null);

            // Assert
            Assert.Equal(mockFlightLogic.Object.FlightId, _sut.CurrentFlightId);
        }

        [Fact]
        public void StationLogicCreated_StationIdReturnsCorrectValue()
        {
            // Arrange
            var station = new Station { StationId = ObjectId.GenerateNewId() };

            // Act
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Assert
            Assert.Equal(station.StationId, _sut.StationId);
        }

        [Fact]
        public async Task ClearAsync_NoFlightSet_ThrowsInvalidOperationException()
        {
            // Arrange
            var station = new Station { StationId = ObjectId.GenerateNewId() };
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ClearAsync(It.IsAny<ObjectId>()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ClearAsync(null));
        }

        [Fact]
        public async Task SetFlightAsync_NoFlightSet_ThrowsInvalidOperationException()
        {
            // Arrange
            var station = new Station { StationId = ObjectId.GenerateNewId() };
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.SetFlightAsync(null!, null));
        }

        [Fact]
        public async Task SetFlightAsync_WhenCalledAfterCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var station = new Station { StationId = ObjectId.GenerateNewId() };
            var cts = new CancellationTokenSource();
            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);
            var mockFlightLogic = new Mock<IFlightLogic>();
            mockFlightLogic
                .Setup(x => x.ThrowIfCancellationRequestedAsync(It.IsAny<CancellationTokenSource>()))
                .Callback(cts.Cancel);

            // Act
            await _sut.SetFlightAsync(mockFlightLogic.Object, cts);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _sut.SetFlightAsync(Mock.Of<IFlightLogic>(), cts));
            mockFlightLogic.Verify(x => x.ThrowIfCancellationRequestedAsync(cts));
        }

        [Fact]
        public async Task SetFlightAsync_WhenCalled_CallsRaiseFlightRunStartedAsyncOnFlight()
        {
            // Arrange
            var station = new Station { StationId = ObjectId.GenerateNewId() };
            var mockFlightLogic = new Mock<IFlightLogic>();

            mockFlightLogic
                .Setup(x => x.RaiseFlightRunStartedAsync(station.StationId))
                .Returns(Task.CompletedTask);

            _sut = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

            // Act
            await _sut.SetFlightAsync(mockFlightLogic.Object);

            // Assert
            mockFlightLogic.Verify(
                x => x.RaiseFlightRunStartedAsync(station.StationId),
                Times.Once(),
                "StationLogic failed to notify the flight that the run started.");
        }

        //[Fact]
        //public async Task SetFlightAsync_WhenCalled_RaiseStationClearedEvent()
        //{
        //    // Arrange
        //    var tcs = new TaskCompletionSource<bool>();
        //    var station = new Station { StationId = ObjectId.GenerateNewId() };
        //    var flightId = ObjectId.GenerateNewId();
        //    var mockPrevStationLogic = new Mock<IStationLogic>();
        //    var mockFlightLogic = new Mock<IFlightLogic>();
        //    mockFlightLogic
        //        .SetupGet(x => x.FlightId)
        //        .Returns(flightId);
        //    _stationLogic = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);
        //    mockFlightLogic
        //        .SetupGet(x => x.FlightId)
        //        .Returns(flightId);
        //    mockFlightLogic
        //        .SetupGet(x => x.RouteId)
        //        .Returns(ObjectId.GenerateNewId());
        //    mockFlightLogic
        //        .SetupGet(x => x.CurrentStation)
        //        .Returns(mockPrevStationLogic.Object);
        //    _onStationClearedAsync = (s, e) =>
        //    {
        //        tcs.SetResult(true);
        //        return Task.CompletedTask;
        //    };
        //    mockPrevStationLogic.Object.StationClearedAsync += _onStationClearedAsync;
        //    mockPrevStationLogic
        //        .Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
        //        .Returns(async () => await mockPrevStationLogic
        //            .RaiseAsync(x => x.StationClearedAsync += null, null!, default!));

        //    // Act
        //    await _stationLogic.SetFlightAsync(mockFlightLogic.Object, null);

        //    // Assert
        //    mockPrevStationLogic.VerifyAdd(
        //        x => x.StationClearedAsync += It.IsAny<AsyncEventHandler<IStationClearedEventArgs>>(),
        //        Times.Once);
        //    bool result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        //    Assert.True(result, "The StationCleared event was not raised within the timeout.");
        //}

        //[Fact]
        //public async Task SetFlightAsync_WhenCalled_CallsClear()
        //{
        //    // Arrange
        //    var flightId = ObjectId.GenerateNewId();
        //    var station = new Station { StationId = ObjectId.GenerateNewId() };
        //    var mockFlightLogic = new Mock<IFlightLogic>();
        //    var mockPrevStationLogic = new Mock<IStationLogic>();

        //    mockFlightLogic
        //        .SetupGet(x => x.FlightId)
        //        .Returns(flightId);
        //    mockFlightLogic
        //        .SetupGet(x => x.RouteId)
        //        .Returns(ObjectId.GenerateNewId());
        //    mockFlightLogic
        //        .SetupGet(x => x.CurrentStation)
        //        .Returns(mockPrevStationLogic.Object);
        //    mockPrevStationLogic
        //        .Setup(x => x.ClearAsync(It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);
        //    _stationLogic = new StationLogic(station, _mockDomainEvents.Object, _mockLogger);

        //    // Act
        //    await _stationLogic.SetFlightAsync(mockFlightLogic.Object);

        //    // Assert
        //    mockPrevStationLogic.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        //}

        public void Dispose()
        {
            if (_sut is not null)
            {
                //_stationLogic.StationOccupiedAsync -= _onStationOccupiedAsync;
                //_stationLogic.StationClearingAsync -= _onStationClearingAsync;
                //_stationLogic.StationClearedAsync -= _onStationClearedAsync;
            }
        }
    }
}
