using Airport.Simulator.Abstractions;
using Airport.Simulator.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace Airport.IntegrationTests
{
    public class AuthTokenHandlerPollyIntegrationTests
    {
        [Fact]
        public async Task PollyRetry_WithAuthTokenHandler_ClearsTokenAndRefetchesOn401()
        {
            // Arrange
            var services = new ServiceCollection();

            var mockAuthService = new Mock<IAuthService>();

            // First time it logs in, return an old token. 
            // Second time (after the 401 triggers a retry and clears it), return a new token.
            mockAuthService.SetupSequence(x => x.LoginAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("old_expired_token")
                .ReturnsAsync("fresh_new_token");

            services.AddSingleton(mockAuthService.Object);
            services.AddTransient<AuthTokenHandler>();

            var mockPrimaryHandler = new MockPrimaryHandler();

            // Set up the exact pipeline: Polly first, then AuthTokenHandler, then the primary handler
            services.AddHttpClient("TestClient")
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => !msg.IsSuccessStatusCode)
                    .RetryAsync(1)) // Just configure Polly to retry once for the test
                .AddHttpMessageHandler<AuthTokenHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => mockPrimaryHandler);

            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("TestClient");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.airport.com/test");
            var response = await client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Primary network handler should have been hit exactly twice (initial + 1 retry)
            mockPrimaryHandler.RequestCount.Should().Be(2);

            // Verify the correct tokens were injected in order
            mockPrimaryHandler.AuthorizationTokens[0].Should().Be("old_expired_token");
            mockPrimaryHandler.AuthorizationTokens[1].Should().Be("fresh_new_token");

            // Verify the auth service was actually called twice to fetch the new token
            mockAuthService.Verify(x => x.LoginAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task PollyRetry_WithWrongHandlerOrder_FailsToUpdateToken()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockAuthService = new Mock<IAuthService>();

            mockAuthService.SetupSequence(x => x.LoginAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("old_expired_token")
                .ReturnsAsync("fresh_new_token");

            services.AddSingleton(mockAuthService.Object);
            services.AddTransient<AuthTokenHandler>();

            var mockPrimaryHandler = new MockPrimaryHandler();

            // Set up the WRONG pipeline: AuthTokenHandler FIRST, Polly SECOND
            services.AddHttpClient("WrongOrderClient")
                .AddHttpMessageHandler<AuthTokenHandler>() // Registered first!
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => !msg.IsSuccessStatusCode)
                    .RetryAsync(1))
                .ConfigurePrimaryHttpMessageHandler(() => mockPrimaryHandler);

            var serviceProvider = services.BuildServiceProvider();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var client = clientFactory.CreateClient("WrongOrderClient");

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.airport.com/test");
            var response = await client.SendAsync(request);

            // Assert
            // The request was successfully retried by Polly and returned 200...
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            mockPrimaryHandler.RequestCount.Should().Be(2);

            // The tokens never changed
            mockPrimaryHandler.AuthorizationTokens[0].Should().Be("old_expired_token");
            mockPrimaryHandler.AuthorizationTokens[1].Should().Be("old_expired_token"); // <--- Proves the token wasn't updated!

            // Prove that the AuthService was only ever called ONE time because AuthTokenHandler was bypassed!
            mockAuthService.Verify(x => x.LoginAsync(It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        // A dummy handler that acts as the physical network layer
        private class MockPrimaryHandler : HttpMessageHandler
        {
            public int RequestCount { get; private set; }
            public List<string?> AuthorizationTokens { get; } = new();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestCount++;
                AuthorizationTokens.Add(request.Headers.Authorization?.Parameter);

                // First request: Simulate the token being expired (401)
                if (RequestCount == 1)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                // Second request (Polly Retry): Return 200 OK
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
