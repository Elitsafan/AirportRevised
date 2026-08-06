using Airport.Models.DTOs;
using Airport.Models.Enums;
using Airport.Simulator.Abstractions;
using Airport.Simulator.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using System.Configuration;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Airport.Simulator.Services
{
    internal class FlightLauncherService : IFlightLauncherService
    {
        #region Fields
        private readonly Random _random;
        private readonly HttpClient _client;
        private readonly IFlightGenerator _flightGenerator;
        private readonly FlightTimeoutConfiguration _flightTimeoutConfiguration;
        private readonly FlightEndPointsConfiguration _flightsConfig;
        private readonly ILogger<FlightLauncherService> _logger;
        #endregion

        public FlightLauncherService(
            HttpClient client,
            IFlightGenerator flightGenerator,
            IOptions<FlightTimeoutConfiguration> flightTimeoutConfiguration,
            IOptions<FlightEndPointsConfiguration> flightsConfiguration,
            ILogger<FlightLauncherService> logger)
        {
            _random = new Random(DateTime.Now.Millisecond);
            _client = client;
            _flightGenerator = flightGenerator;
            _flightTimeoutConfiguration = flightTimeoutConfiguration.Value;
            _flightsConfig = flightsConfiguration.Value;
            _logger = logger;
            ValidateFlightsConfiguration();
            _client.BaseAddress = new Uri(_flightsConfig.BaseUrl!);
        }

        // Launches multiple flights 
        public async IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(
            int n = 6,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var flights = _flightGenerator.GenerateFlights(n);
            ct.ThrowIfCancellationRequested();
            Func<FlightForCreationDTO, Task<HttpResponseMessage>> task = async flight =>
            {
                ct.ThrowIfCancellationRequested();
                _logger.LogInformation("Launching {FlightType}...", flight.FlightType);
                return flight.FlightType == FlightType.Landing
                    ? await _client.PostAsJsonAsync(
                        _flightsConfig.Landing,
                        flight,
                        ct)
                    : await _client.PostAsJsonAsync(
                        _flightsConfig.Departure,
                        flight,
                        ct);
            };
            foreach (var flight in flights
                .Select(flight => Task.Run(() => task(flight), ct)))
                yield return await flight;
            yield break;
        }

        // Launches multiple flights 
        // Accepts args[0] is a number
        public async IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(params string[]? args)
        {
            // Input validation
            if (args?.Length == 0)
                yield break;
            if (!int.TryParse(args?[0], out int numOfFlights) ||
                numOfFlights <= 0)
                throw new ArgumentException("First argument is invalid. Only non-negative numbers are allowed.");

            // Getnerates flights
            var flights = _flightGenerator.GenerateFlights(numOfFlights)
                .Select(f => Task.Run(async () => await LaunchOneAsync(f)))
                .ToArray();
            _logger.LogInformation("Launching many flights...");
            foreach (var flight in flights)
                yield return await flight;
            yield break;
        }

        public async Task<HttpResponseMessage> LaunchOneAsync(FlightForCreationDTO flight, CancellationToken ct = default)
        {
            _logger.LogInformation("Launching {FlightType}...", flight.FlightType);

            return flight.FlightType == FlightType.Landing
                ? await _client.PostAsJsonAsync(
                    _flightsConfig.Landing,
                    flight,
                    ct)
                : await _client.PostAsJsonAsync(
                    _flightsConfig.Departure,
                    flight,
                    ct);
        }
        // Launches a flight according to _flightTimeoutConfiguration.Timeout
        public async Task SetFlightTimeoutAsync(FlightType? flightType, CancellationToken ct = default)
        {
            using var periodicTimer = new PeriodicTimer(_flightTimeoutConfiguration.SendFlightTimeout);

            while (await periodicTimer.WaitForNextTickAsync(ct))
            {
                var flight = _flightGenerator.GenerateFlight(flightType ?? (_random.Next() % 2 == 0
                    ? FlightType.Landing
                    : FlightType.Departure));
                /*var result = */
                LaunchOneAsync(flight, ct).Forget();
                //_logger.LogInformation("{result}", await result.Content.ReadAsStringAsync(ct));
            }
        }

        public async Task StartStandbyModeAsync(CancellationToken ct = default)
        {
            var periodicTimer = new PeriodicTimer(_flightTimeoutConfiguration.StandbyTimeout);
            while (await periodicTimer.WaitForNextTickAsync(ct))
                await foreach (var result in LaunchManyAsync(_flightTimeoutConfiguration.AutoFlightCount, ct))
                    _logger.LogInformation("{result}", await result.Content.ReadAsStringAsync(ct));
        }

        public void Dispose() => _client?.Dispose();

        private void ValidateFlightsConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_flightsConfig?.BaseUrl) ||
                string.IsNullOrWhiteSpace(_flightsConfig?.Start) ||
                string.IsNullOrWhiteSpace(_flightsConfig?.Departure) ||
                string.IsNullOrWhiteSpace(_flightsConfig?.Landing)/* ||
                string.IsNullOrWhiteSpace(_flightsConfig?.Flights)*/)
                throw new ConfigurationErrorsException(
                    "Values for Start/AddFlight endpoints are missing.\n" +
                    "Please provide any in the configuration file and start again.");
        }
    }
}
