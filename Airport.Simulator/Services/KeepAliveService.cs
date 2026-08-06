using Airport.Simulator.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Configuration;

namespace Airport.Simulator.Services
{
    public class KeepAliveService : BackgroundService
    {
        #region Fields
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _client;
        private readonly ILogger<KeepAliveService> _logger;
        private readonly FlightTimeoutConfiguration _flightTimeoutConfiguration;
        private readonly FlightEndPointsConfiguration _flightsEndpointsConfig;
        #endregion

        public KeepAliveService(
            IHttpClientFactory httpClientFactory,
            IOptions<FlightTimeoutConfiguration> flightTimeoutConfig,
            IOptions<FlightEndPointsConfiguration> flightEndpointsConfig,
            ILogger<KeepAliveService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _client = _httpClientFactory.CreateClient(nameof(KeepAliveService));
            _flightTimeoutConfiguration = flightTimeoutConfig.Value;
            _flightsEndpointsConfig = flightEndpointsConfig.Value;
            ValidateFlightEndpointsConfiguration();
            _logger = logger;
            _client.BaseAddress = new Uri(_flightsEndpointsConfig.BaseUrl!);
        }

        public override void Dispose()
        {
            _client.Dispose();

            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using var periodicTimer = new PeriodicTimer(_flightTimeoutConfiguration.KeepAliveInterval);

            try
            {
                var startResponse = await StartApiAsync(ct);

                _logger.LogInformation("Starting Airport Simulator...");
                _logger.LogInformation("Start response received with status: {StatusCode}", startResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Initial API startup failed after retries: {Message}", ex.Message);
            }

            while (await periodicTimer.WaitForNextTickAsync(ct))
                try
                {
                    var result = await StartApiAsync(ct);

                    _logger.LogInformation("{result}", await result.Content.ReadAsStringAsync(ct));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Keep-alive ping failed: {Message}", ex.Message);
                }
        }

        private void ValidateFlightEndpointsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_flightsEndpointsConfig.BaseUrl) ||
                string.IsNullOrWhiteSpace(_flightsEndpointsConfig.Start))
                throw new ConfigurationErrorsException(
                    "Values for BaseUrl/Start endpoints are missing.\n" +
                    "Please provide any in the configuration file and start again.");
        }

        private async Task<HttpResponseMessage> StartApiAsync(CancellationToken ct = default) =>
            await _client.GetAsync(_flightsEndpointsConfig.Start, ct);
    }
}
