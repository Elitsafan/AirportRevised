using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace Airport.Presentation.Tests.Controllers
{
    public class AuthControllerTests
    {
        #region Fields
        private readonly IFixture _fixture;
        private readonly Mock<IAuthService> _mockAithuService;
        private readonly AuthController _sut;
        #endregion

        public AuthControllerTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
            _mockAithuService = _fixture.Freeze<Mock<IAuthService>>();
            _sut = _fixture.Create<AuthController>();
        }

        [Fact]
        public void LoginValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginCredentials = _fixture.Create<LoginCredentials>();
            var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy_token.signature";

            _mockAithuService
                .Setup(x => x.Login(loginCredentials.Username, loginCredentials.Password))
                .Returns(expectedToken);

            // Act
            var result = _sut.Login(loginCredentials);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            okResult.Value.Should().BeEquivalentTo(new { Token = expectedToken });
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginCredentials = _fixture.Create<LoginCredentials>();

            _mockAithuService
                .Setup(x => x.Login(loginCredentials.Username, loginCredentials.Password))
                .Returns((string?)null);

            // Act
            var result = _sut.Login(loginCredentials);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedResult>().Subject;
        }
    }
}
