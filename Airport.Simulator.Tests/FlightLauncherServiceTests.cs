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
        public async Task StartAsync_WhenCalled_StartsLauncher()
        {
            // Arrange
            var fepc = new FlightEndPointsConfiguration
            {
                BaseUrl = BASE_URL,
                Start = "/api/Airport/Start",
                Landing = "/api/Flights/AddLanding",
                Departure = "/api/Flights/AddDeparture"
            };
            _mockFlightEndpointsConfig
                .SetupGet(x => x.Value)
                .Returns(fepc);
            using var client = new HttpClient(_mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri(fepc.BaseUrl);
            var mockedProtected = _mockHttpMessageHandler.Protected();
            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    nameof(HttpClient.SendAsync),
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get &&
                    m.RequestUri == new Uri(fepc.BaseUrl + fepc.Start)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("OK")
                });
            var sut = new FlightLauncherService(
                client,
                _mockFlightGenerator.Object,
                _mockFlightTimeoutConfig.Object,
                _mockFlightEndpointsConfig.Object,
                _mockLogger);

            // Act
            var response = await sut.StartAsync();

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK);
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
                Assert.True(launch.StatusCode == HttpStatusCode.Created);
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
                Assert.True(launch.StatusCode == HttpStatusCode.Created);
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
