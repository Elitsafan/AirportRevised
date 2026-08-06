namespace Airport.Simulator.Tests
{
    public class FlightLauncherServiceTests
    {
        #region Fields
#if DEBUG
        private const string BASE_URL = "https://localhost:5005";
#elif !DEBUG
        private const string BASE_URL = "https://airport.api.elitzafan.com"; 
#endif
        private readonly Mock<IFlightGenerator> _mockFlightGenerator;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IOptions<FlightTimeoutConfiguration>> _mockFlightTimeoutConfig;
        private readonly Mock<IOptions<FlightEndPointsConfiguration>> _mockFlightEndpointsConfig;
        private readonly ILogger<FlightLauncherService> _mockLogger;
        #endregion

        public FlightLauncherServiceTests()
        {
            _mockFlightGenerator = new Mock<IFlightGenerator>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockFlightTimeoutConfig = new Mock<IOptions<FlightTimeoutConfiguration>>();
            _mockFlightEndpointsConfig = new Mock<IOptions<FlightEndPointsConfiguration>>();
            _mockLogger = Mock.Of<ILogger<FlightLauncherService>>();
        }

        [Fact]
        public async Task LaunchManyAsync_WhenCalled_LaunchesFlights()
        {
            // Arrange
            var fepc = new FlightEndPointsConfiguration
            {
                BaseUrl = "http://localhost:5005",
                Start = "/api/Airport/Start",
                Landing = "/api/Flights/Landing",
                Departure = "/api/Flights/Departure"
            };

            _mockFlightEndpointsConfig
                .SetupGet(x => x.Value)
                .Returns(fepc);

            var flights = new List<FlightForCreationDTO>
            {
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
            };

            var mockedProtected = _mockHttpMessageHandler.Protected();

            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    nameof(HttpClient.SendAsync),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            using var client = new HttpClient(_mockHttpMessageHandler.Object);

            client.BaseAddress = new Uri(fepc.BaseUrl);

            var sut = new FlightLauncherService(
                client,
                _mockFlightGenerator.Object,
                _mockFlightTimeoutConfig.Object,
                _mockFlightEndpointsConfig.Object,
                _mockLogger);

            _mockFlightGenerator
                .Setup(x => x.GenerateFlights(It.IsAny<int>()))
                .Returns(flights);

            // Act & Assert
            await foreach (var launch in sut.LaunchManyAsync(10))
                Assert.Equal(HttpStatusCode.Created, launch.StatusCode);
        }

        [Fact]
        public async Task LaunchManyAsync_WithParams_WhenCalled_LaunchesFlights()
        {
            // Arrange
            var fepc = new FlightEndPointsConfiguration
            {
                BaseUrl = "http://localhost:5005",
                Start = "/api/Airport/Start",
                Landing = "/api/Flights/Landing",
                Departure = "/api/Flights/Departure"
            };

            _mockFlightEndpointsConfig
                .SetupGet(x => x.Value)
                .Returns(fepc);

            var flights = new List<FlightForCreationDTO>
            {
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
            };

            var mockedProtected = _mockHttpMessageHandler.Protected();

            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    nameof(HttpClient.SendAsync),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            using var client = new HttpClient(_mockHttpMessageHandler.Object);

            client.BaseAddress = new Uri(fepc.BaseUrl);

            var sut = new FlightLauncherService(
                client,
                _mockFlightGenerator.Object,
                _mockFlightTimeoutConfig.Object,
                _mockFlightEndpointsConfig.Object,
                _mockLogger);

            _mockFlightGenerator
                .Setup(x => x.GenerateFlights(It.IsAny<int>()))
                .Returns(flights);
            _mockFlightGenerator
                .Setup(x => x.GenerateFlight(FlightType.Departure))
                .Returns(new DepartureForCreationDTO());
            _mockFlightGenerator
                .Setup(x => x.GenerateFlight(FlightType.Landing))
                .Returns(new LandingForCreationDTO());

            // Act & Assert
            await foreach (var launch in sut.LaunchManyAsync("7"))
                Assert.Equal(HttpStatusCode.Created, launch.StatusCode);
        }

        [Fact]
        public async Task LaunchManyAsync_WithParams_WhenCalled_LaunchesFlightsAndExit()
        {
            // Arrange
            var fepc = new FlightEndPointsConfiguration
            {
                BaseUrl = "http://localhost:5005",
                Start = "/api/Airport/Start",
                Landing = "/api/Flights/Landing",
                Departure = "/api/Flights/Departure"
            };

            _mockFlightEndpointsConfig
                .SetupGet(x => x.Value)
                .Returns(fepc);

            var flights = new List<FlightForCreationDTO>
            {
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO(),
                new LandingForCreationDTO(),
                new DepartureForCreationDTO()
            };

            var mockedProtected = _mockHttpMessageHandler.Protected();

            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    nameof(HttpClient.SendAsync),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            using var client = new HttpClient(_mockHttpMessageHandler.Object);

            client.BaseAddress = new Uri(fepc.BaseUrl);

            var sut = new FlightLauncherService(
                client,
                _mockFlightGenerator.Object,
                _mockFlightTimeoutConfig.Object,
                _mockFlightEndpointsConfig.Object,
                _mockLogger);

            _mockFlightGenerator
                .Setup(x => x.GenerateFlights(It.IsAny<int>()))
                .Returns(flights);

            // Act & Assert
            await foreach (var launch in sut.LaunchManyAsync(10))
                Assert.Equal(HttpStatusCode.Created, launch.StatusCode);
        }
    }
}
