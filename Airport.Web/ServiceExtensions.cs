using Airport.Contracts.Factories;
using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.Domain.Factories;
using Airport.Domain.Helpers;
using Airport.Domain.Providers;
using Airport.Domain.Repositories;
using Airport.Persistence.Repositories;
using Airport.Services;
using Airport.Services.Abstractions;

namespace Airport.Web
{
    public static class ServiceExtensions
    {
        public static void AddAirportServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            services.AddScoped<IFlightService, FlightService>();
            services.AddScoped<IStationService, StationService>();
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IAirportService, AirportService>();

            services.AddSingleton<IDomainEvents, DomainEvents>();

            services.AddSingleton<IFlightLogicFactory, FlightLogicFactory>();
            services.AddSingleton<IStationLogicFactory, StationLogicFactory>();
            services.AddSingleton<IDirectionLogicFactory, DirectionLogicFactory>();
            services.AddSingleton<IRouteLogicFactory, RouteLogicFactory>();

            services.AddSingleton<IAirportStateProvider, AirportStateProvider>();
            services.AddSingleton<IStationLogicProvider, StationLogicProvider>();
            services.AddSingleton<IDirectionLogicProvider, DirectionLogicProvider>();
            services.AddSingleton<IRouteLogicProvider, RouteLogicProvider>();

            services.AddHostedService<AirportHubService>();
            services.AddHostedService<FlightEventHandlers>();
        }
    }
}
