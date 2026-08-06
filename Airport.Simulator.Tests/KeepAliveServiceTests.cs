namespace Airport.Simulator.Tests
{
    public class KeepAliveServiceTests
    {
        #region Fields
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IOptions<FlightTimeoutConfiguration>> _mockFlightTimeoutConfig;
        private readonly Mock<IOptions<FlightEndPointsConfiguration>> _mockFlightEndpointsConfig;
        private readonly ILogger<KeepAliveService> _mockLogger;
        #endregion

        public KeepAliveServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockFlightTimeoutConfig = new Mock<IOptions<FlightTimeoutConfiguration>>();
            _mockFlightEndpointsConfig = new Mock<IOptions<FlightEndPointsConfiguration>>();
            _mockLogger = Mock.Of<ILogger<KeepAliveService>>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenStarted_StartsService()
        {
            // Arrange
            var fepc = new FlightEndPointsConfiguration
            {
                BaseUrl = "http://localhost:5005",
                Start = "/api/Airport/Start"
            };

            var ftc = new FlightTimeoutConfiguration
            {
                KeepAliveInterval = TimeSpan.FromMilliseconds(50),
            };

            _mockFlightEndpointsConfig
                .SetupGet(x => x.Value)
                .Returns(fepc);
            _mockFlightTimeoutConfig
                .SetupGet(x => x.Value)
                .Returns(ftc);

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

            using var httpClient = new HttpClient(_mockHttpMessageHandler.Object);

            httpClient.BaseAddress = new Uri(fepc.BaseUrl);

            _mockHttpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            using var sut = new KeepAliveService(
                _mockHttpClientFactory.Object,
                _mockFlightTimeoutConfig.Object,
                _mockFlightEndpointsConfig.Object,
                _mockLogger);

            using var cts = new CancellationTokenSource();

            // Act
            var task = sut.StartAsync(cts.Token);
            await Task.Delay(500);
            await task;
            await cts.CancelAsync();

            // Assert
            _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()));

            mockedProtected.Verify(
                nameof(HttpClient.SendAsync),
                Times.AtLeastOnce(),
                new object[]
                {
                    ItExpr.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get &&
                    m.RequestUri == new Uri(fepc.BaseUrl + fepc.Start)),
                    ItExpr.IsAny<CancellationToken>()
                });
        }
    }
}
