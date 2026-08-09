using Airport.Models.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Airport.IntegrationTests
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsSuccessAndToken()
        {
            // Arrange
            var loginCredentials = new LoginCredentials
            {
                Username = "admin",
                Password = "password123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginCredentials);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("token");
        }
    }
}
