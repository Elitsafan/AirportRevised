using Airport.Persistence;
using DnsClient.Internal;
using Microsoft.Extensions.Options;

namespace Airport.Services.Services
{
    public class BackgroundSaverService : BackgroundService
    {
        #region Fields
        private readonly IRepositoryManager _repoManager;
        private readonly IOptions<AirportDbConfiguration> _dbConfiguration;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<BackgroundSaverService> _logger;
        #endregion

        public BackgroundSaverService(
            IRepositoryManager repoManager,
            IDomainEvents domainEvents,
            IOptions<AirportDbConfiguration> dbConfiguration,
            ILogger<BackgroundSaverService> logger)
        {
            _repoManager = repoManager;
            _domainEvents = domainEvents;
            _dbConfiguration = dbConfiguration;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken ct)
        {
            _domainEvents.SystemResetRequested += CleanFlightsAsync;

            await base.StartAsync(ct);
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            _domainEvents.SystemResetRequested -= CleanFlightsAsync;

            await base.StopAsync(ct);
        }

        public override void Dispose()
        {
            _domainEvents.SystemResetRequested -= CleanFlightsAsync;

            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var periodicTimer = new PeriodicTimer(_dbConfiguration.Value.FlushInterval);
            try
            {
                while (await periodicTimer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        await FlushFlightsAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while flushing flights to database");
                    }

                    try
                    {
                        await RemoveOldFlightsAsync();
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

            await CleanFlightsAsync();
        }

        private async Task CleanFlightsAsync()
        {
            while (await FlushFlightsAsync() > 0)
                await RemoveOldFlightsAsync();
        }

        private async Task RemoveOldFlightsAsync(CancellationToken ct = default)
        {
            var countRemoved = await _repoManager.FlightRepository.EnforceStorageLimitAsync();

            if (countRemoved > 0)
                _logger.LogInformation("{CountRemoved} old flights removed from database.", countRemoved);
        }

        private async Task<long> FlushFlightsAsync(CancellationToken ct = default)
        {
            var countFlushed = await _repoManager.FlightRepository.FlushAsync();

            if (countFlushed > 0)
                _logger.LogInformation("{CountFlushed} flights flushed to database.", countFlushed);

            return countFlushed;
        }
    }
}
