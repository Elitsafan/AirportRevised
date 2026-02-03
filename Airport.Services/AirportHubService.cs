using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.EventArgs.StationEventArgs;
using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Services
{
    public class AirportHubService : IHostedService
    {
        #region Fields
        private readonly IHubContext<AirportHub> _hub;
        private readonly IDomainEvents _domainEvents;
        private readonly IStationLogicProvider _stationLogicProvider;
        private readonly ILogger<AirportHubService> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        public AirportHubService(
            IStationLogicProvider stationLogicProvider,
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
            _stationLogicProvider = stationLogicProvider;
        }

        public async Task StartAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunDone += OnFlightRunDoneAsync;
            _stationLogicProvider.AnyStationOccupied += OnStationOccupiedAsync;
            _stationLogicProvider.AnyStationCleared += OnStationClearedAsync;
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunDone -= OnFlightRunDoneAsync;
            _stationLogicProvider.AnyStationOccupied -= OnStationOccupiedAsync;
            _stationLogicProvider.AnyStationCleared -= OnStationClearedAsync;
            await Task.CompletedTask;
        }

        protected virtual async Task OnFlightRunDoneAsync(object? sender, IFlightRunDoneEventArgs e) =>
            await _hub.Clients.All.SendCoreAsync(
                nameof(IDomainEvents.FlightRunDone),
                new[] { JsonConvert.SerializeObject(e.Flight.FlightId, _jsonSerializerSettings) });

        protected virtual async Task OnStationOccupiedAsync(
            object? sender,
            IStationStateChangedEventArgs<IStationChangedData> e) => await OnStationChangedAsync(
                nameof(IDomainEvents.StationOccupied),
                e.StationsState.ToList());

        protected virtual async Task OnStationClearedAsync(
            object? sender,
            IStationStateChangedEventArgs<IStationChangedData> e) => await OnStationChangedAsync(
                nameof(IDomainEvents.StationCleared),
                e.StationsState.ToList());

        private async Task OnStationChangedAsync(string name, IEnumerable<IStationChangedData> data) =>
            await _hub.Clients.All.SendCoreAsync(
                name,
                new[] { JsonConvert.SerializeObject(data, _jsonSerializerSettings) });
    }
}
