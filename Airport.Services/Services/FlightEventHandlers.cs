//#define TEST
using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.Helpers;
#if TEST
using Airport.Domain.Helpers;
#endif
using Airport.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Airport.Services.Services
{
    public class FlightEventHandlers : BackgroundService
    {
        #region Fields
        private readonly IDomainEvents _domainEvents;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FlightEventHandlers> _logger;
        #endregion

        public FlightEventHandlers(
            IDomainEvents domainEvents,
            IServiceScopeFactory scopeFactory,
            ILogger<FlightEventHandlers> logger)
        {
            _domainEvents = domainEvents;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _domainEvents.FlightRunStarted += OnFlightRunStartedAsync;
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _domainEvents.FlightRunStarted -= OnFlightRunStartedAsync;
            await base.StopAsync(cancellationToken);
        }

        private async Task OnFlightRunStartedAsync(object? sender, IFlightRunStartedEventArgs args)
        {
#if TEST
            _logger.LogCritical($"<----- {args.Flight.FlightId} | {Guid.NewGuid()} ----->");
#endif
            using var scope = _scopeFactory.CreateScope();
            var repositoryManager = scope.ServiceProvider.GetRequiredService<IRepositoryManager>();
            args.Flight.RouteId = args.RouteId;
            await repositoryManager.FlightRepository.AddOneAsync(args.Flight);
#if TEST
            _logger.LogInformation($"{args.Flight.ToFlightType()} ID: {args.Flight.FlightId} -----> Registered");
#endif
        }
    }
}
