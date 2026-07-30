//#define TEST
using Airport.Contracts.Factories;
using Airport.Contracts.Logics;
using Airport.Domain.Exceptions;
using Airport.Services.Abstractions;
#if TEST
using Airport.Domain.Helpers;
#endif

namespace Airport.Services.Services
{
    public class FlightProcessingService : BackgroundService
    {
        #region Fields
        private readonly IFlightQueue _queue;
        private readonly IFlightLogicFactory _flightFactory;
        private readonly IRepositoryManager _repoManager;
        private readonly ILogger<FlightProcessingService> _logger;
        #endregion

        public FlightProcessingService(
            IFlightQueue queue,
            IRepositoryManager repoManager,
            IFlightLogicFactory flightFactory,
            ILogger<FlightProcessingService> logger)
        {
            _queue = queue;
            _repoManager = repoManager;
            _flightFactory = flightFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await foreach (var flight in _queue.ReadAllAsync(ct))
                _ = RunFlightSafeAsync(flight, ct);
        }

        private async Task RunFlightSafeAsync(Flight flight, CancellationToken ct)
        {
            IFlightLogic? flightLogic = null;

            try
            {
                using var cts = new CancellationTokenSource();

                flightLogic = (await _flightFactory.GetCreatorAsync(flight, ct)).Create();

                await flightLogic.RunAsync(cts.Token);

                _repoManager.FlightRepository.AddCompletedFlight(flight);
#if TEST
            _logger.LogInformation("{FlightType} ID: {FlightId} -----> Unegistered", flightLogic!.FlightType, flightLogic.FlightId);
#endif
                await flightLogic.RaiseFlightRunDoneAsync(cts.Token);
            }
            catch (StationEntranceFailedException ex)
            {
                _logger.LogError(ex, "Error processing flight {Id}", flight.FlightId);

                throw new InvalidOperationException("Could not proceed with flight run.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error processing flight {Id}", flight.FlightId);

                throw;
            }
            finally
            {
                flightLogic?.Dispose();
            }
        }
    }
}
