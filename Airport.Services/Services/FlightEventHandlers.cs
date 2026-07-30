//#define TEST
using Airport.Contracts.EventArgs.FlightEventArgs;
#if TEST
using Airport.Domain.Helpers;
#endif

namespace Airport.Services.Services
{
    public class FlightEventHandlers : BackgroundService
    {
        #region Fields
        private readonly IDomainEvents _domainEvents;
        private readonly IRepositoryManager _repoManager;
        private readonly ILogger<FlightEventHandlers> _logger;
        #endregion

        public FlightEventHandlers(IRepositoryManager repoManager, IDomainEvents domainEvents, ILogger<FlightEventHandlers> logger)
        {
            _domainEvents = domainEvents;
            _repoManager = repoManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunStarted += OnFlightRunStartedAsync;

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunStarted -= OnFlightRunStartedAsync;

            await base.StopAsync(ct);
        }

        private async Task OnFlightRunStartedAsync(object? sender, IFlightRunStartedEventArgs args)
        {
            args.Flight.RouteId = args.RouteId;

            await _repoManager.FlightRepository.AddOneAsync(args.Flight);
#if TEST
            _logger.LogInformation("{FlightType} ID: {FlightId} -----> Registered", args.Flight.ToFlightType(), args.Flight.FlightId);
#endif
        }
    }
}
