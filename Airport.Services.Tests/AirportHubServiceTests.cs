using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.Helpers;
using Airport.Services.Services;
using Airport.Services.Tests.Stubs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Services.Tests
{
    public class AirportHubServiceTests
    {
        #region Fields
        private readonly Mock<IHubContext<AirportHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<IDomainEvents> _mockDomainEvents;
        private readonly Mock<IStationLogicProvider> _mockStationProvider;
        private readonly Mock<ILogger<AirportHubService>> _mockLogger;
        private readonly AirportHubService _sut;
        #endregion

        public AirportHubServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<AirportHub>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
            _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

            _mockDomainEvents = new Mock<IDomainEvents>();
            _mockStationProvider = new Mock<IStationLogicProvider>();
            _mockLogger = new Mock<ILogger<AirportHubService>>();

            _sut = new AirportHubService(
                _mockDomainEvents.Object,
                _mockStationProvider.Object,
                _mockHubContext.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task FlightRunStarted_ShouldProcessStation_AndSendSignalRMessage()
        {
            // Arrange
            var mockEventArgs = new Mock<IFlightRunStartedEventArgs>();

            var fakeStationData = new StationChangedDataStub
            {
                StationId = ObjectId.GenerateNewId(),
            };
            var expectedDataList = new List<IStationChangedData> { fakeStationData };

            _mockStationProvider
                .Setup(p => p.ProcessFlightStartedAsync(mockEventArgs.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDataList);

            // Act
            await _sut.StartAsync(default);

            await _mockDomainEvents.RaiseAsync(
                e => e.FlightRunStarted += null,
                this,
                mockEventArgs.Object);

            // Assert
            _mockStationProvider.Verify(
                p => p.ProcessFlightStartedAsync(mockEventArgs.Object, It.IsAny<CancellationToken>()),
                Times.Once);

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    nameof(IDomainEvents.FlightRunStarted),
                    It.Is<object[]>(args => args.Length == 1 && args[0] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task StopAsync_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            await _sut.StartAsync(CancellationToken.None);

            var mockFlightStartedEventArgs = new Mock<IFlightRunStartedEventArgs>();
            var mockFlightDoneEventArgs = new Mock<IFlightRunDoneEventArgs>();

            // Act
            await _sut.StopAsync(CancellationToken.None);

            _mockDomainEvents.Object.FlightRunStarted += (sender, args) => Task.CompletedTask;
            _mockDomainEvents.Object.FlightRunDone += (sender, args) => Task.CompletedTask;

            await _mockDomainEvents.RaiseAsync(
                e => e.FlightRunStarted += null,
                this,
                mockFlightStartedEventArgs.Object);

            await _mockDomainEvents.RaiseAsync(
                e => e.FlightRunDone += null,
                this,
                mockFlightDoneEventArgs.Object);

            // Assert
            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
