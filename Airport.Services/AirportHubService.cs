using Airport.Contracts.EventArgs;
using Airport.Contracts.Helpers;
using Airport.Contracts.Logics;
using Airport.Contracts.Providers;
using Airport.Services.Abstractions;
using Airport.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Services
{
    public class AirportHubService : IAirportHubService
    {
        #region Fields
        private readonly IStationLogicProvider _stationLogicProvider;
        private readonly IHubContext<AirportHub> _hub;
        private readonly ILogger<AirportHubService> _logger;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        #endregion

        public AirportHubService(
            IDomainEvents domainEvents,
            IStationLogicProvider stationLogicProvider,
            ILogger<AirportHubService> logger,
            IHubContext<AirportHub> hub)
        {
            _hub = hub;
            _logger = logger;
            _stationLogicProvider = stationLogicProvider;
            _jsonSerializerSettings = new()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };

            //domainEvents.DataRefreshed += OnDataRefreshedAsync;
            _stationLogicProvider.AnyStationOccupied += OnStationOccupiedAsync;
            _stationLogicProvider.AnyStationCleared += OnStationClearedAsync;
        }

        public void RegisterFlightRunDone(IFlightLogic flightLogic) =>
            flightLogic.FlightRunDone += OnFlightRunDoneAsync;

        /// <summary>
        /// Sends the flight id when the flight run ends, and unregisters listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnFlightRunDoneAsync(object? sender, IFlightRunDoneEventArgs e)
        {
            try
            {
                await _hub.Clients.All.SendCoreAsync(
                    nameof(IFlightLogic.FlightRunDone),
                    new[] { JsonConvert.SerializeObject(e.Flight.FlightId, _jsonSerializerSettings) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending message to clients");
                throw;
            }
            finally { e.Flight.FlightRunDone -= OnFlightRunDoneAsync; }
        }

        private async Task OnStationOccupiedAsync(
            object? sender,
            IStationChangedEventArgs<IStationChangedData> e) => await OnStationChangedAsync(
                nameof(IStationLogic.StationOccupiedAsync),
                e.StationsState);

        private async Task OnStationClearedAsync(
            object? sender,
            IStationChangedEventArgs<IStationChangedData> e) => await OnStationChangedAsync(
                nameof(IStationLogic.StationClearedAsync),
                e.StationsState);

        private async Task OnStationChangedAsync(string name, IEnumerable<IStationChangedData> data) =>
            await _hub.Clients.All.SendCoreAsync(
                name,
                new[] { JsonConvert.SerializeObject(data.ToList(), _jsonSerializerSettings) });

        private Task OnDataRefreshedAsync()
        {
            _stationLogicProvider.AnyStationOccupied += OnStationOccupiedAsync;
            _stationLogicProvider.AnyStationCleared += OnStationClearedAsync;
            return Task.CompletedTask;
        }
    }
}
