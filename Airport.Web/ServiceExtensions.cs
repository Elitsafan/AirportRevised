using Airport.Contracts.Factories;
using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.Domain.Factories;
using Airport.Domain.Helpers;
using Airport.Domain.Providers;
using Airport.Domain.Repositories;
using Airport.Persistence.Repositories;
using Airport.Services.Abstractions;
using Airport.Services.Channels;
using Airport.Services.Services;

namespace Airport.Web
{
    public static class ServiceExtensions
    {
        public static void AddAirportServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddHostedService<AirportHubService>();
            services.AddHostedService<BackgroundSaverService>();
            services.AddHostedService<FlightEventHandlers>();
            services.AddHostedService<FlightProcessingService>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IFlightService, FlightService>();
            services.AddScoped<IStationService, StationService>();
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IAirportService, AirportService>();

            services.AddSingleton<IRepositoryManager, RepositoryManager>();

            services.AddSingleton<IDomainEvents, DomainEvents>();

            services.AddSingleton<IRouteValidator, RouteValidator>();

            services.AddSingleton<IFlightLogicFactory, FlightLogicFactory>();
            services.AddSingleton<IStationLogicFactory, StationLogicFactory>();
            services.AddSingleton<IDirectionLogicFactory, DirectionLogicFactory>();
            services.AddSingleton<IRouteLogicFactory, RouteLogicFactory>();
            services.AddSingleton<ISyncerLogicFactory, SyncerLogicFactory>();
            services.AddSingleton<ISectionLogicFactory, SectionLogicFactory>();
            services.AddSingleton<IFlightQueue, FlightQueue>();

            services.AddSingleton<ISyncerLogicProvider, SyncerLogicProvider>();
            services.AddSingleton<ISectionLogicProvider, SectionLogicProvider>();
            services.AddSingleton<IAirportStateProvider, AirportStateProvider>();
            services.AddSingleton<IStationLogicProvider, StationLogicProvider>();
            services.AddSingleton<IDirectionLogicProvider, DirectionLogicProvider>();
            services.AddSingleton<IRouteLogicProvider, RouteLogicProvider>();
        }
    }
}
