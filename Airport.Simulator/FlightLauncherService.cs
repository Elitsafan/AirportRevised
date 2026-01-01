using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;
using Airport.Models.DTOs;
using Airport.Models.Enums;
using Airport.Simulator.Abstractions;
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
        private readonly ILogger<FlightLauncherService> _logger;
        private readonly IFlightGenerator _flightGenerator;
        private readonly IFlightTimeoutConfiguration _flightTimeoutConfiguration;
        private readonly IFlightEndPointsConfiguration _flightsConfig;
        #endregion

        public FlightLauncherService(
            HttpClient client,
            ILogger<FlightLauncherService> logger,
            IFlightGenerator flightGenerator,
            IFlightTimeoutConfiguration flightTimeoutConfiguration,
            IFlightEndPointsConfiguration flightsConfiguration)
        {
            _random = new Random(DateTime.Now.Millisecond);
            _logger = logger;
            _client = client;
            _flightGenerator = flightGenerator;
            _flightTimeoutConfiguration = flightTimeoutConfiguration;
            _flightsConfig = flightsConfiguration;
            ValidateFlightsConfiguration();
            _client.BaseAddress = new Uri(_flightsConfig.BaseUrl!);
        }

        // Launches multiple flights 
        public async IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(
            int n = 6,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var flights = _flightGenerator.GenerateFlights(n);
            cancellationToken.ThrowIfCancellationRequested();
            Func<FlightForCreationDTO, Task<HttpResponseMessage>> task = async flight =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation($"Launching {flight.FlightType}...");
                var id = ObjectId.GenerateNewId(DateTime.Now);
                return flight.FlightType == FlightType.Landing
                    ? await _client.PostAsJsonAsync(
                        $"{_flightsConfig.Landing}/{id}",
                        flight,
                        cancellationToken)
                    : await _client.PostAsJsonAsync($"{_flightsConfig.Departure}/{id}",
                        flight,
                        cancellationToken);
            };
            foreach (var flight in flights
                .Select(flight => Task.Run(() => task(flight), cancellationToken)))
                yield return await flight;
            yield break;
        }

        // Launches multiple flights 
        // Accepts args[0] is a number and args[1](optional) is "exit"
        public async IAsyncEnumerable<HttpResponseMessage> LaunchManyAsync(params string[]? args)
        {
            // Input validation
            if (args is null || args.Length == 0)
                yield break;
            if (!int.TryParse(args[0], out int numOfFlights) ||
                numOfFlights <= 0)
                throw new ArgumentException("First argument is invalid. Only non-negative numbers are allowed.");

            // Getnerates flights
            var flights = _flightGenerator.GenerateFlights(numOfFlights)
                .Select(f => Task.Run(async () => await LaunchOneAsync(f)))
                .ToArray();
            _logger.LogInformation($"Launching many flights...");
            foreach (var flight in flights)
                yield return await flight;
            yield break;
        }
        // Send a request to Start endpoint
        public async Task<HttpResponseMessage> StartAsync(CancellationToken cancellationToken = default) =>
            await _client.GetAsync(_flightsConfig.Start, cancellationToken);

        public async Task<HttpResponseMessage> LaunchOneAsync(
            FlightForCreationDTO flight,
            CancellationToken cancellationToken = default)
        {
            var id = ObjectId.GenerateNewId(DateTime.Now);
            _logger.LogInformation($"Launching {flight.FlightType}...");
            return flight.FlightType == FlightType.Landing
                ? await _client.PostAsJsonAsync(
                    $"{_flightsConfig.Landing}/{id}",
                    flight,
                    cancellationToken)
                : await _client.PostAsJsonAsync(
                    $"{_flightsConfig.Departure}/{id}",
                    flight,
                    cancellationToken);
        }
        // Launches a flight according to _flightTimeoutConfiguration.Timeout
        public async Task SetFlightTimeoutAsync(FlightType? flightType, CancellationToken cancellationToken = default)
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_flightTimeoutConfiguration.Timeout));
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                var flight = _flightGenerator.GenerateFlight(flightType ?? (_random.Next() % 2 == 0
                    ? FlightType.Landing
                    : FlightType.Departure));
                LaunchOneAsync(flight, cancellationToken).Forget();
                //_logger.LogInformation(result.ToString());
            }
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            await ValueTask.CompletedTask;
        }

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
