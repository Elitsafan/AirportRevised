using Airport.Services.Services;
using FluentAssertions;

namespace Airport.Services.Tests
{
    public class AuthServiceTests
    {
        #region Fields
        private readonly IAuthService _sut;
        private readonly JwtSettings _jwtSettings;
        private readonly LoginCredentials _loginCredentials; 
        #endregion

        public AuthServiceTests()
        {
            // Manual arrangement since this service relies purely on configuration objects
            _jwtSettings = new JwtSettings
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Key = "SuperSecretTestKeyThatIsAtLeast32BytesLong!",
                ExpiryMinutes = 60
            };

            _loginCredentials = new LoginCredentials
            {
                Username = "admin",
                Password = "password123"
            };

            var jwtOptions = Options.Create(_jwtSettings);
            var loginOptions = Options.Create(_loginCredentials);

            _sut = new AuthService(jwtOptions, loginOptions);
        }

        [Fact]
        public void Login_ValidCredentials_ReturnsJwtToken()
        {
            // Act
            var result = _sut.Login("admin", "password123");

            // Assert
            result.Should().NotBeNullOrWhiteSpace();

            // Basic validation that it looks like a JWT (3 parts separated by dots)
            var tokenParts = result!.Split('.');
            tokenParts.Should().HaveCount(3);
        }

        [Theory]
        [InlineData("admin", "wrongpassword")]
        [InlineData("wronguser", "password123")]
        [InlineData("", "")]
        public void Login_InvalidCredentials_ReturnsNull(string username, string password)
        {
            // Act
            var result = _sut.Login(username, password);

            // Assert
            result.Should().BeNull();
        }
    }
}
