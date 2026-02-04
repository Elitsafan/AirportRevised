using Airport.Domain.Repositories;
using Airport.Persistence;
using DnsClient.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Airport.Services.Services
{
    public class BackgroundSaverService : BackgroundService
    {
        #region Fields
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<AirportDbConfiguration> _dbConfiguration;
        private readonly ILogger<BackgroundSaverService> _logger;
        #endregion

        public BackgroundSaverService(
            IServiceScopeFactory scopeFactory,
            IOptions<AirportDbConfiguration> dbConfiguration,
            ILogger<BackgroundSaverService> logger)
        {
            _scopeFactory = scopeFactory;
            _dbConfiguration = dbConfiguration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var periodicTimer = new PeriodicTimer(_dbConfiguration.Value.FlushInterval);
            try
            {
                while (await periodicTimer.WaitForNextTickAsync(ct))
                {
                    await using var loopScope = _scopeFactory.CreateAsyncScope();
                    var loopRepoManager = loopScope.ServiceProvider.GetRequiredService<IRepositoryManager>();
                    var loopFlightRepo = loopRepoManager.FlightRepository;

                    try
                    {
                        await FlushFlightsAsync(loopFlightRepo, ct);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while flushing flights to database");
                    }

                    try
                    {
                        await RemoveOldFlightsAsync(loopFlightRepo, ct);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while flights removing old flights from database");
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                _logger.LogError(e, "Service stopping...");
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var repoManager = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();
            var flightRepo = repoManager.FlightRepository;

            while (await FlushFlightsAsync(flightRepo) > 0)
                await RemoveOldFlightsAsync(flightRepo); 
        }

        private async Task RemoveOldFlightsAsync(IFlightRepository flightRepo, CancellationToken ct = default)
        {
            var countRemoved = await flightRepo.EnforceStorageLimitAsync(ct);
            if (countRemoved > 0)
                _logger.LogInformation($"{countRemoved} old flights removed from database.");
        }

        private async Task<long> FlushFlightsAsync(IFlightRepository flightRepo, CancellationToken ct = default)
        {
            var countFlushed = await flightRepo.FlushAsync(ct);
            if (countFlushed > 0)
                _logger.LogInformation($"{countFlushed} flights flushed to database.");
            return countFlushed;
        }
    }
}
