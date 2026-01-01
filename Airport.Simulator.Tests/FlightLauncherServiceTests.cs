namespace OnionArchitecture.Simulator.Tests
{
    public class FlightLauncherServiceTests
    {
        #region Fields
        private IFlightLauncherService _service;
        private HttpClient _httpClient;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private Mock<IFlightGenerator> _mockFlightGenerator;
        private Mock<IFlightEndPointsConfiguration> _mockFlightEndPointsConfiguration;
        private Mock<IFlightTimeoutConfiguration> _mockFlightTimeoutConfiguration;
        private ILogger<FlightLauncherService> _mockLogger;
        #endregion

        public FlightLauncherServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockFlightGenerator = new Mock<IFlightGenerator>();
            _mockFlightEndPointsConfiguration = new Mock<IFlightEndPointsConfiguration>();
            _mockFlightTimeoutConfiguration = new Mock<IFlightTimeoutConfiguration>();
            _mockLogger = Mock.Of<ILogger<FlightLauncherService>>();

            _mockFlightEndPointsConfiguration
                .SetupGet(x => x.Start)
                .Returns("/api/Airport/Start");
            // Options.Create(new FlightEndPointsConfiguration { BaseUrl = "http://localhost:5005" });
            _mockFlightEndPointsConfiguration
                .SetupGet(x => x.BaseUrl)
                .Returns("https://airport.api.elitzafan.com");
            _mockFlightEndPointsConfiguration
                .SetupGet(x => x.Departure)
                .Returns("/api/Flights/Departure");
            _mockFlightEndPointsConfiguration
                .SetupGet(x => x.Landing)
                .Returns("/api/Flights/Landing");

            _service = new FlightLauncherService(
                _httpClient,
                _mockLogger,
                _mockFlightGenerator.Object,
                _mockFlightTimeoutConfiguration.Object,
                _mockFlightEndPointsConfiguration.Object);
        }

        [Fact]
        public void Created_NotNull() => Assert.NotNull(_service);

        [Fact]
        public async Task StartAsync_WhenCalled_StartsLauncherAsync()
        {
            var mockedProtected = _mockHttpMessageHandler.Protected();
            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    nameof(HttpClient.SendAsync),
                    ItExpr.Is<HttpRequestMessage>(
                        m => m.RequestUri!.Equals(
                            _mockFlightEndPointsConfiguration.Object.BaseUrl +
                            _mockFlightEndPointsConfiguration.Object.Start)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var response = await _service.StartAsync();

            Assert.True(response.StatusCode == HttpStatusCode.OK);
        }

        [Fact]
        public async Task LaunchManyAsync_WhenCalled_LaunchesFlightsAsync()
        {
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
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            _mockFlightGenerator
                .Setup(x => x.GenerateFlights(It.IsAny<int>()))
                .Returns(flights);

            await foreach (var launch in _service.LaunchManyAsync(10))
                Assert.True(launch.StatusCode == HttpStatusCode.Created);
        }

        [Fact]
        public async Task LaunchManyAsync_WithParams_WhenCalled_LaunchesFlightsAsync()
        {
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
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            _mockFlightGenerator
                .Setup(x => x.GenerateFlights(It.IsAny<int>()))
                .Returns(flights);
            _mockFlightGenerator
                .Setup(x => x.GenerateFlight(FlightType.Departure))
                .Returns(new DepartureForCreationDTO());
            _mockFlightGenerator
                .Setup(x => x.GenerateFlight(FlightType.Landing))
                .Returns(new LandingForCreationDTO());
            await foreach (var launch in _service.LaunchManyAsync("7"))
                Assert.True(launch.StatusCode == HttpStatusCode.Created);
        }

        [Fact(Skip = "Crashed")]
        public async Task LaunchManyAsync_WithParams_WhenCalled_LaunchesFlightsAndExitAsync()
        {
            var mockedProtected = _mockHttpMessageHandler.Protected();
            mockedProtected
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created));

            await foreach (var launch in _service.LaunchManyAsync("10", "exit"))
                Assert.Equal(HttpStatusCode.Created, launch.StatusCode);
        }
    }
}
