using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.SignalR;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Services.Services
{
    public class AirportHubService : IHostedService
    {
        #region Fields
        private readonly IHubContext<AirportHub> _hub;
        private readonly IDomainEvents _domainEvents;
        private readonly IStationLogicProvider _stationProvider;
        private readonly ILogger<AirportHubService> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        public AirportHubService(
            IStationLogicProvider stationProvider,
            IDomainEvents domainEvents,
            ILogger<AirportHubService> logger,
            IHubContext<AirportHub> hub)
        {
            _hub = hub;
            _domainEvents = domainEvents;
            _logger = logger;
            _jsonSerializerSettings = new()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };
            _stationProvider = stationProvider;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunDone += OnFlightRunDoneAsync;
            _domainEvents.FlightRunStarted += OnFlightRunStartedAsync;
            _domainEvents.StationCleared += OnStationClearedAsync;

            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunDone -= OnFlightRunDoneAsync;
            _domainEvents.FlightRunStarted -= OnFlightRunStartedAsync;
            _domainEvents.StationCleared -= OnStationClearedAsync;

            await Task.CompletedTask;
        }

        protected virtual async Task OnFlightRunStartedAsync(
            object? sender,
            IFlightRunStartedEventArgs e) => await OnStationChangedAsync(
                nameof(IDomainEvents.FlightRunStarted),
                (await _stationProvider.ProcessFlightStartedAsync(e)).ToList());

        protected virtual async Task OnFlightRunDoneAsync(object? sender, IFlightRunDoneEventArgs e) =>
            await _hub.Clients.All.SendCoreAsync(
                nameof(IDomainEvents.FlightRunDone),
                new[] { JsonConvert.SerializeObject(e.Flight.FlightId, _jsonSerializerSettings) });

        protected virtual async Task OnStationClearedAsync(
            object? sender,
            IStationClearedEventArgs e) => await OnStationChangedAsync(
                nameof(IDomainEvents.StationCleared),
                (await _stationProvider.ProcessStationClearedAsync(e)).ToList());

        private async Task OnStationChangedAsync(string name, IEnumerable<IStationChangedData> data) =>
            await _hub.Clients.All.SendCoreAsync(
                name,
                new[] { JsonConvert.SerializeObject(data, _jsonSerializerSettings) });
    }
}
