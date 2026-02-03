//#define TEST
using Airport.Contracts.EventArgs.FlightEventArgs;
using Airport.Contracts.Helpers;
using Airport.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Airport.Services
{
    public class FlightEventHandlers : IHostedService
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

        public async Task StartAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunStarted += OnFlightRunStartedAsync;
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct)
        {
            _domainEvents.FlightRunStarted -= OnFlightRunStartedAsync;
            await Task.CompletedTask;
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
