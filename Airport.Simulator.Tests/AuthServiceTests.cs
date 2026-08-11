using FluentAssertions;
using System.Text.Json;

namespace Airport.Simulator.Tests
{
    public class AuthServiceTests
    {
        #region Fields
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly IOptions<Configurations.LoginCredentials> _loginCredentials;
        private readonly IOptions<AuthEndpoints> _authEndpoints; 
        #endregion

        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:5005")
            };

            _mockLogger = new Mock<ILogger<AuthService>>();

            _loginCredentials = Options.Create(new Configurations.LoginCredentials { Username = "admin", Password = "password" });

            _authEndpoints = Options.Create(new AuthEndpoints 
            {
                BaseUrl = "http://localhost:5005",
                Login = "/api/auth/login" 
            });

            _sut = new AuthService(_httpClient, _authEndpoints, _loginCredentials, _mockLogger.Object);
        }

        [Fact]
        public async Task LoginAsync_SuccessfulLogin_ReturnsToken()
        {
            // Arrange
            var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy_token";

            var responseJson = JsonSerializer.Serialize(new { Token = expectedToken });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _sut.LoginAsync();

            // Assert
            result.Should().Be(expectedToken);
        }

        [Fact]
        public async Task LoginAsync_FailedLogin_ReturnsNull()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            // Act
            var result = await _sut.LoginAsync();

            // Assert
            result.Should().BeNull();
        }
    }
}
