using Airport.Contracts.Helpers;
using Airport.Domain.EventArgs;
using Airport.Services.Tests.Stubs;
using Microsoft.VisualStudio.Threading;

namespace Airport.Services.Tests
{
    public class AirportHubServiceTests
    {
        #region Fields
        private AirportHubService _sut = null!;
        private readonly ILogger<AirportHubService> _mockLogger;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<IClientProxy> _mockClientsProxy;
        private readonly Mock<IHubContext<AirportHub>> _mockHubContext;
        private readonly Mock<IStationLogicProvider> _mockStationLogicProvider;
        #endregion

        public AirportHubServiceTests()
        {
            _mockLogger = Mock.Of<ILogger<AirportHubService>>();
            _mockHubContext = new Mock<IHubContext<AirportHub>>();
            _mockHubClients = new Mock<IHubClients>();
            _mockClientsProxy = new Mock<IClientProxy>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
        }

        [Fact]
        public async Task RegisterFlightRunDone_HandlerRegisteredAsync()
        {
            // Arrange
            var mockFlightLogic = new Mock<IFlightLogic>();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var flightId = ObjectId.GenerateNewId();
            var mockEventArgs = new Mock<IFlightRunDoneEventArgs>();
            _sut = new AirportHubService(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            _mockHubClients
                .SetupGet(x => x.All)
                .Returns(_mockClientsProxy.Object);
            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(_mockHubClients.Object);
            _mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IFlightLogic.FlightRunDone),
                    new object[] { JsonConvert.SerializeObject(flightId, jsonSerializerSettings) },
                    CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockEventArgs
                .SetupGet(x => x.Flight)
                .Returns(mockFlightLogic.Object);

            // Act
            _sut.RegisterFlightRunDone(mockFlightLogic.Object);
            await mockFlightLogic
                .RaiseAsync(x => x.FlightRunDone += null, null!, mockEventArgs.Object);

            // Assert
            _mockClientsProxy.Verify();
        }

        [Fact]
        public async Task StationOccupiedAsyncEvent_Raised_CallsSendCoreAsync()
        {
            // Arrange
            var mockEventArgs = new Mock<IStationChangedEventArgs<IStationChangedData>>();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var expectedData = new List<IStationChangedData>
            { 
                new StationChangedDataStub
                {
                    StationId = ObjectId.GenerateNewId(),
                    Flight = new FlightInfoStub
                    {
                        FlightId = ObjectId.GenerateNewId(),
                        FlightType = FlightType.Landing
                    }
                }
            }
            .AsQueryable();
            var expectedJson = JsonConvert.SerializeObject(expectedData, jsonSerializerSettings);
            mockEventArgs
                .SetupGet(x => x.StationsState)
                .Returns(expectedData);

            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(_mockHubClients.Object);
            _mockHubClients
                .SetupGet(x => x.All)
                .Returns(_mockClientsProxy.Object);
            _mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IStationLogic.StationOccupiedAsync),
                    It.Is<object[]>(args => args[0].ToString() == expectedJson),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _sut = new AirportHubService(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            // Act
            await _mockStationLogicProvider.RaiseAsync(
                x => x.AnyStationOccupied += null,
                null!,
                mockEventArgs.Object);

            // Assert
            _mockClientsProxy.Verify();
        }

        [Fact]
        public async Task StationClearedAsyncEvent_Raised_CallsSendCoreAsync()
        {
            // Arrange
            var mockEventArgs = new Mock<IStationChangedEventArgs<IStationChangedData>>();
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var expectedData = new List<IStationChangedData>
            {
                new StationChangedDataStub
                {
                    StationId = ObjectId.GenerateNewId(),
                    Flight = new FlightInfoStub
                    {
                        FlightId = ObjectId.GenerateNewId(),
                        FlightType = FlightType.Landing
                    }
                }
            }
            .AsQueryable();
            var expectedJson = JsonConvert.SerializeObject(expectedData, jsonSerializerSettings);
            mockEventArgs
                .SetupGet(x => x.StationsState)
                .Returns(expectedData);

            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(_mockHubClients.Object);
            _mockHubClients
                .SetupGet(x => x.All)
                .Returns(_mockClientsProxy.Object);
            _mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IStationLogic.StationClearedAsync),
                    It.Is<object[]>(args => args[0].ToString() == expectedJson),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            _sut = new AirportHubService(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            // Act
            await _mockStationLogicProvider.RaiseAsync(
                x => x.AnyStationCleared += null,
                null!,
                mockEventArgs.Object);

            // Assert
            _mockClientsProxy.Verify();
        }
    }
}
