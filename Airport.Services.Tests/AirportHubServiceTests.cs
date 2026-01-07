namespace Airport.Services.Tests
{
    public class AirportHubServiceTests
    {
        #region Fields
        private ILogger<AirportHubService> _mockLogger;
        private Mock<IHubContext<AirportHub>> _mockHubContext;
        private Mock<IStationLogicProvider> _mockStationLogicProvider;
        private Mock<IFlightLogic> _mockFlightLogic;
        #endregion

        public AirportHubServiceTests()
        {
            _mockLogger = Mock.Of<ILogger<AirportHubService>>();
            _mockHubContext = new Mock<IHubContext<AirportHub>>();
            _mockStationLogicProvider = new Mock<IStationLogicProvider>();
            _mockFlightLogic = new Mock<IFlightLogic>();
        }

        [Fact]
        public async Task AirportHubService_Created_NotNullAsync()
        {
            var airportHubService = await AirportHubService.CreateAsync(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            Assert.NotNull(airportHubService);
        }

        [Fact]
        public async Task RegisterFlightRunDone_HandlerRegisteredAsync()
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var flightId = ObjectId.GenerateNewId();
            var mockEventArgs = new Mock<IFlightRunDoneEventArgs>();
            var mockHubClients = new Mock<IHubClients>();
            var mockClientsProxy = new Mock<IClientProxy>();
            var airportHubService = await AirportHubService.CreateAsync(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            airportHubService.RegisterFlightRunDone(_mockFlightLogic.Object);

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            mockHubClients
                .SetupGet(x => x.All)
                .Returns(mockClientsProxy.Object);
            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(mockHubClients.Object);
            mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IFlightLogic.FlightRunDone),
                    new object[] { JsonConvert.SerializeObject(flightId, jsonSerializerSettings) },
                    CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockEventArgs
                .SetupGet(x => x.Flight)
                .Returns(_mockFlightLogic.Object);

            await _mockFlightLogic
                .RaiseAsync(x => x.FlightRunDone += null, null!, mockEventArgs.Object);

            mockClientsProxy.Verify();
        }

        [Fact]
        public async Task StationOccupiedAsyncEvent_Raised_CallsSendCoreAsync()
        {
            var stationId = ObjectId.GenerateNewId();
            var flightId = ObjectId.GenerateNewId();
            var flightType = FlightType.Landing;
            var expected = Enumerable.Repeat(new
            {
                stationId,
                flight = new
                {
                    flightId,
                    flightType
                }
            },
            1);
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var mockEventArgs = new Mock<IStationOccupiedEventArgs>();
            var mockStationLogic = new Mock<IStationLogic>();
            var mockHubClients = new Mock<IHubClients>();
            var mockClientsProxy = new Mock<IClientProxy>();

            _mockStationLogicProvider
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[] { mockStationLogic.Object });
            var airportHubService = await AirportHubService.CreateAsync(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            mockHubClients
                .SetupGet(x => x.All)
                .Returns(mockClientsProxy.Object);
            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(mockHubClients.Object);
            mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IStationLogic.StationOccupiedAsync),
                    new object[] { JsonConvert.SerializeObject(expected, jsonSerializerSettings) },
                    CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockStationLogic
                .SetupGet(x => x.CurrentFlightId)
                .Returns(flightId);
            mockStationLogic
                .SetupGet(x => x.CurrentFlightType)
                .Returns(flightType);
            mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(stationId);

            await mockStationLogic
                .RaiseAsync(x => x.StationOccupiedAsync += null, null!, mockEventArgs.Object);

            mockClientsProxy.Verify();
        }

        [Fact]
        public async Task StationClearedAsyncEvent_Raised_CallsSendCoreAsync()
        {
            var stationId = ObjectId.GenerateNewId();
            var flightId = ObjectId.GenerateNewId();
            var flightType = FlightType.Landing;
            var expected = Enumerable.Repeat(new
            {
                stationId,
                flight = new
                {
                    flightId,
                    flightType
                }
            },
            1);
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            var mockEventArgs = new Mock<IStationClearedEventArgs>();
            var mockStationLogic = new Mock<IStationLogic>();
            var mockHubClients = new Mock<IHubClients>();
            var mockClientsProxy = new Mock<IClientProxy>();

            _mockStationLogicProvider
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IStationLogic[] { mockStationLogic.Object });
            var airportHubService = await AirportHubService.CreateAsync(
                _mockStationLogicProvider.Object,
                _mockLogger,
                _mockHubContext.Object);

            _mockFlightLogic
                .SetupGet(x => x.FlightId)
                .Returns(flightId);
            mockHubClients
                .SetupGet(x => x.All)
                .Returns(mockClientsProxy.Object);
            _mockHubContext
                .SetupGet(x => x.Clients)
                .Returns(mockHubClients.Object);
            mockClientsProxy
                .Setup(x => x.SendCoreAsync(
                    nameof(IStationLogic.StationClearedAsync),
                    new object[] { JsonConvert.SerializeObject(expected, jsonSerializerSettings) },
                    CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
            mockStationLogic
                .SetupGet(x => x.CurrentFlightId)
                .Returns(flightId);
            mockStationLogic
                .SetupGet(x => x.CurrentFlightType)
                .Returns(flightType);
            mockStationLogic
                .SetupGet(x => x.StationId)
                .Returns(stationId);

            await mockStationLogic
                .RaiseAsync(x => x.StationClearedAsync += null, null!, mockEventArgs.Object);

            mockClientsProxy.Verify();
        }
    }
}
