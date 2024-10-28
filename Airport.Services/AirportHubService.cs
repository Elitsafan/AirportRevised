using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Airport.Contracts.EventArgs;
using Airport.Contracts.Logics;
using Airport.Contracts.Providers;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using Airport.SignalR;

namespace Airport.Services
{
    public class AirportHubService : IAirportHubService
    {
        #region Fields
        private IHubContext<AirportHub> _hub = null!;
        private IStationLogicProvider _stationLogicProvider = null!;
        private ILogger<AirportHubService> _logger = null!;
        private JsonSerializerSettings _jsonSerializerSettings = null!;
        private IQueryable<StationChangedData> _stationsData = null!;
        #endregion

        public static async Task<AirportHubService> CreateAsync(
            IStationLogicProvider stationLogicProvider,
            ILogger<AirportHubService> logger,
            IHubContext<AirportHub> hub) => await new AirportHubService().InitializeAsync(
                stationLogicProvider,
                logger,
                hub);

        public void RegisterFlightRunDone(IFlightLogic flightLogic) =>
            flightLogic.FlightRunDone += OnFlightRunDoneAsync;

        private AirportHubService()
        {
        }

        private async Task<AirportHubService> InitializeAsync(
            IStationLogicProvider stationLogicProvider,
            ILogger<AirportHubService> logger,
            IHubContext<AirportHub> hub)
        {
            _hub = hub;
            _stationLogicProvider = stationLogicProvider;
            _logger = logger;
            _jsonSerializerSettings = new()
            {
                Formatting = Formatting.Indented,
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
            };

            // Prepare stations query for sending the state of stations
            var stationLogics = await _stationLogicProvider.GetAllAsync();
            _stationsData = stationLogics
                .OrderBy(s => s.StationId)
                .Select(s => new StationChangedData
                {
                    StationId = s.StationId,
                    Flight = s.CurrentFlightId is null
                        ? null
                        : new FlightInfo
                        {
                            FlightId = s.CurrentFlightId,
                            FlightType = s.CurrentFlightType
                        },
                })
                .AsQueryable();

            foreach (var stationLogic in stationLogics)
            {
                stationLogic.StationOccupiedAsync += OnStationOccupiedAsync;
                stationLogic.StationClearedAsync += OnStationClearedAsync;
            }
            return this;
        }

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
                    new object[]
                    {
                        JsonConvert.SerializeObject(e.Flight.FlightId, _jsonSerializerSettings)
                    });
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
            IStationOccupiedEventArgs e) => await OnStationChangedAsync(nameof(IStationLogic.StationOccupiedAsync));

        private async Task OnStationClearedAsync(
            object? sender,
            IStationClearedEventArgs e) => await OnStationChangedAsync(nameof(IStationLogic.StationClearedAsync));

        private async Task OnStationChangedAsync(string name)
        {
            try
            {
                await _hub.Clients.All.SendCoreAsync(
                    name,
                    new object[]
                    {
                        JsonConvert.SerializeObject(_stationsData, _jsonSerializerSettings)
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending message to clients");
                throw;
            }
        }

        private class StationChangedData
        {
            public ObjectId StationId { get; set; }
            public FlightInfo? Flight { get; set; }
        }
        private class FlightInfo
        {
            public ObjectId? FlightId { get; set; }
            public FlightType? FlightType { get; set; }
        }
    }
}
