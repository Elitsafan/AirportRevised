using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq.Contrib.HttpClient;
using Polly;
using Polly.Extensions.Http;

namespace Airport.Simulator.Tests
{
    public class SimulatorIntegrationTests
    {
        private readonly string _baseUrl = "https://mock-api.com";

        [Fact]
        public async Task KeepAliveService_WhenStarted_ShouldPingApiPeriodically()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightEndPointsConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));

                    services.Configure<FlightTimeoutConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightTimeoutConfiguration)));

                    services.AddHostedService<KeepAliveService>();

                    services.AddHttpClient(nameof(KeepAliveService))
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object)
                        .AddPolicyHandler(HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .OrResult(msg => !msg.IsSuccessStatusCode)
                            .WaitAndRetryAsync(3, _ => TimeSpan.Zero));
                })
                .Build();

            var configuration = host.Services.GetRequiredService<IOptions<FlightEndPointsConfiguration>>().Value;

            handler
                .SetupRequestSequence(HttpMethod.Get, configuration.BaseUrl + configuration.Start)
                .ReturnsResponse(HttpStatusCode.InternalServerError)
                .ReturnsResponse(HttpStatusCode.InternalServerError)
                .ReturnsResponse(HttpStatusCode.OK);

            // Act
            var keepAliveService = host.Services
                .GetServices<IHostedService>()
                .OfType<KeepAliveService>()
                .Single();

            await keepAliveService.StartAsync(default);

            await Task.Delay(1000);

            await keepAliveService.StopAsync(default);

            // Assert
            handler.VerifyRequest(
                HttpMethod.Get, configuration.BaseUrl + configuration.Start,
                Times.Exactly(3));
        }

        [Fact]
        public async Task FlightLauncherService_WhenGeneratedFlightIsLanding_ShouldPostToLandingEndpoint()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightEndPointsConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));

                    services
                        .AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object);
                })
                .Build();

            var configuration = host.Services.GetRequiredService<IOptions<FlightEndPointsConfiguration>>().Value;

            handler
                .SetupRequest(HttpMethod.Post, configuration.BaseUrl + configuration.Landing)
                .ReturnsResponse(HttpStatusCode.Accepted);

            // Act
            var flightLauncherService = host.Services.GetRequiredService<IFlightLauncherService>();

            var response = await flightLauncherService.LaunchOneAsync(new LandingForCreationDTO());

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            handler.VerifyRequest(
                HttpMethod.Post, configuration.BaseUrl + configuration.Landing,
                Times.Once());
            handler.VerifyRequest(
                HttpMethod.Post, configuration.BaseUrl + configuration.Departure,
                Times.Never());
        }

        [Fact]
        public async Task FlightLauncherService_WhenGeneratedFlightIsDeparture_ShouldPostToDepartureEndpoint()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightEndPointsConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));

                    services
                        .AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object);
                })
                .Build();

            var configuration = host.Services.GetRequiredService<IOptions<FlightEndPointsConfiguration>>().Value;

            handler
                .SetupRequest(HttpMethod.Post, configuration.BaseUrl + configuration.Departure)
                .ReturnsResponse(HttpStatusCode.Accepted);

            // Act
            var flightLauncherService = host.Services.GetRequiredService<IFlightLauncherService>();

            var response = await flightLauncherService.LaunchOneAsync(new DepartureForCreationDTO());

            // Assert
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            handler.VerifyRequest(
                HttpMethod.Post, configuration.BaseUrl + configuration.Departure,
                Times.Once());
            handler.VerifyRequest(
                HttpMethod.Post, configuration.BaseUrl + configuration.Landing,
                Times.Never());
        }

        [Fact]
        public void HostConfiguration_ShouldCorrectlyBindFlightTimeoutConfiguration_FromAppSettings()
        {
            // Arrange
            TimeSpan keepAliveInterval = new(0, 0, 8, 0, 0); ;
            TimeSpan sendFlightTimeout = new(0, 0, 0, 1, 0); ;
            TimeSpan standbyTimeout = new(0, 0, 9, 0, 0);
            int autoFlightCount = 10;

            var handler = new Mock<HttpMessageHandler>();

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightTimeoutConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightTimeoutConfiguration)));

                    services
                        .AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object);
                })
                .Build();

            // Act
            var configuration = host.Services.GetRequiredService<IOptions<FlightTimeoutConfiguration>>().Value;

            // Assert
            Assert.Equal(keepAliveInterval, configuration.KeepAliveInterval);
            Assert.Equal(autoFlightCount, configuration.AutoFlightCount);
            Assert.Equal(sendFlightTimeout, configuration.SendFlightTimeout);
            Assert.Equal(standbyTimeout, configuration.StandbyTimeout);
        }

        [Fact]
        public void HostConfiguration_ShouldCorrectlyBindFlightEndpoints_FromAppSettings()
        {
            // Arrange
            string startEP = "/api/Airport/Start";
            string addLandingEP = "/api/Flights/AddLanding";
            string addDepartureEP = "/api/Flights/AddDeparture";
            string flightsEP = "/api/Flights";

            var handler = new Mock<HttpMessageHandler>();

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightEndPointsConfiguration>(
                        ctx.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));

                    services
                        .AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object);
                })
                .Build();

            // Act
            var configuration = host.Services.GetRequiredService<IOptions<FlightEndPointsConfiguration>>().Value;

            // Assert
            Assert.Equal(_baseUrl, configuration.BaseUrl);
            Assert.Equal(startEP, configuration.Start);
            Assert.Equal(flightsEP, configuration.Flights);
            Assert.Equal(addDepartureEP, configuration.Departure);
            Assert.Equal(addLandingEP, configuration.Landing);
        }

        [Fact]
        public async Task FlightLauncher_ShouldGenerateFlights()
        {
            // Arrange
            var baseUri = new Uri(_baseUrl);
            string startEP = "/api/Airport/Start";
            string addLandingEP = "/api/Flights/AddLanding";
            string addDepartureEP = "/api/Flights/AddDeparture";

            var handler = new Mock<HttpMessageHandler>();

            handler
                .SetupRequest(HttpMethod.Post, baseUri.OriginalString + addLandingEP)
                .ReturnsResponse(HttpStatusCode.Accepted);

            handler
                .SetupRequest(HttpMethod.Post, baseUri.OriginalString + addDepartureEP)
                .ReturnsResponse(HttpStatusCode.Accepted);

            var host = Host
                .CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddScoped<IFlightGenerator, FlightGenerator>();

                    services.Configure<FlightEndPointsConfiguration>(config =>
                    {
                        config.BaseUrl = baseUri.OriginalString;
                        config.Start = startEP;
                        config.Landing = addLandingEP;
                        config.Departure = addDepartureEP;
                    });

                    services
                        .AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler.Object);
                })
                .Build();

            var flightLauncherService = host.Services.GetRequiredService<IFlightLauncherService>();

            // Act
            await flightLauncherService.LaunchManyAsync(new[] { "1" }).ToListAsync();

            // Assert
            handler.VerifyRequest(
                HttpMethod.Post, baseUri.OriginalString + addLandingEP,
                Times.AtMostOnce());
            handler.VerifyRequest(
                HttpMethod.Post, baseUri.OriginalString + addDepartureEP,
                Times.AtMostOnce());
        }
    }
}
