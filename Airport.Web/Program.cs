using Airport.Contracts.Factories;
using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.Domain.Factories;
using Airport.Domain.Helpers;
using Airport.Domain.Providers;
using Airport.Domain.Repositories;
using Airport.Persistence;
using Airport.Persistence.Repositories;
using Airport.Services;
using Airport.Services.Abstractions;
using Airport.Services.MappingConfigurations;
using Airport.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Airport.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<AirportDbConfiguration>(
                builder.Configuration.GetSection(nameof(AirportDbConfiguration)));
            //#if DEBUG
            //            builder.Logging
            //                .ClearProviders()
            //                .AddEventLog(eventLogSettings =>
            //                {
            //                    eventLogSettings.SourceName = "AirportApplication";
            //                    eventLogSettings.LogName = "AirportLog";
            //                })
            //                .AddConsole();
            //#endif
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {// This tells Swagger to treat ObjectId as a simple string schema
                options.MapType<ObjectId>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "hex",
                    Example = new OpenApiString("6962bc27216b2f3897a15ad0")
                });
            });
            builder.Services.AddSignalR(/*options => options.EnableDetailedErrors = true*/);
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    corsBuilder =>
                    {
                        var clientOrigins = builder.Configuration
                            .GetSection("ClientOrigins")
                            .GetChildren()
                            .Select(cs => cs.Value)!
                            .ToArray()!;
                        corsBuilder.WithOrigins(clientOrigins!)
                            .AllowAnyHeader()
                            .WithMethods("GET", "POST")
                            .AllowCredentials();
                    });
            });
            builder.Services.AddControllers()
                .AddNewtonsoftJson()
                .AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly);
            builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
            builder.Services.AddScoped<IFlightService, FlightService>();
            builder.Services.AddScoped<IStationService, StationService>();
            builder.Services.AddScoped<IRouteService, RouteService>();
            builder.Services.AddScoped<IAirportService, AirportService>();
            builder.Services.AddSingleton<IDomainEvents>(serviceProvider => new DomainEvents());
            builder.Services.AddSingleton<IStationLogicProvider>(serviceProvider =>
            {
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                var domainEvents = serviceProvider.GetRequiredService<IDomainEvents>();
                var logger = serviceProvider.GetRequiredService<ILogger<StationLogicProvider>>();
                return StationLogicProvider.CreateAsync(serviceProvider, cache, domainEvents, logger).Result;
            });
            builder.Services.AddSingleton<IAirportHubService, AirportHubService>(serviceProvider =>
            {
                var stationLogicProvider = serviceProvider.GetRequiredService<IStationLogicProvider>();
                var logger = serviceProvider.GetRequiredService<ILogger<AirportHubService>>();
                var hub = serviceProvider.GetRequiredService<IHubContext<AirportHub>>();
                return AirportHubService.CreateAsync(stationLogicProvider, logger, hub).Result;
            });
            builder.Services.AddSingleton<IDirectionLogicFactory, DirectionLogicFactory>();
            builder.Services.AddSingleton<IFlightLogicFactory, FlightLogicFactory>();
            builder.Services.AddSingleton<IRouteLogicFactory, RouteLogicFactory>();
            builder.Services.AddSingleton<IStationLogicFactory, StationLogicFactory>();
            builder.Services.AddSingleton<IDirectionLogicProvider, DirectionLogicProvider>(
                serviceProvider => DirectionLogicProvider.CreateAsync(serviceProvider).Result);
            builder.Services.AddSingleton<IRouteLogicProvider, RouteLogicProvider>(
                serviceProvider => RouteLogicProvider.CreateAsync(serviceProvider).Result);
            builder.Services.AddSingleton<IMongoClient>(provider =>
            {
                // Not using builder.Configuration - ignores runtime environment variables
                var config = provider.GetRequiredService<IOptions<AirportDbConfiguration>>().Value
                    ?? throw new InvalidOperationException("Database connection string is missing");
                var settings = MongoClientSettings.FromConnectionString(config.ConnectionString);
                settings.ConnectTimeout = TimeSpan.FromMinutes(1);
                settings.MaxConnectionPoolSize = 25;
                settings.MinConnectionPoolSize = 5;
                return new MongoClient(settings);
            });
            builder.Services.AddAutoMapper(cfg =>
            {
                var autoMapperKey = builder.Configuration.GetSection("AutoMapper")["Key"];
                cfg.LicenseKey = autoMapperKey;
                cfg.AddProfile<FlightProfile>();
                cfg.AddProfile<StationProfile>();
                cfg.AddProfile<RouteProfile>();
                cfg.AddProfile<DirectionProfile>();
            });
            builder.Services.AddTransient<ExceptionHandlingMiddleware>();
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024;
            });

            using var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                await SeedDatabaseAsync(app);
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                // For Demo purpose
                await SeedDatabaseAsync(app);
                app.UseHsts();
            }

            app.UseCors();
            app.UseAuthorization();
            app.MapHub<AirportHub>("/airporthub");
            app.MapControllers();
            await app.StartAsync();
            await app.WaitForShutdownAsync();
        }

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
            var dbConfiguration = scope.ServiceProvider.GetRequiredService<IOptions<AirportDbConfiguration>>();
            try
            {
                await SeedData.DeleteAsync(client, dbConfiguration);
                await SeedData.InitializeAsync(client, dbConfiguration);

                // Refresh the station logics cache after seeding
                var domainEvents = scope.ServiceProvider.GetRequiredService<IDomainEvents>();
                await domainEvents.RaiseDataRefreshedAsync();
            }
            catch (TimeoutException ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "A database seeding timeout occurred.");
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "A database seeding error occurred.");
            }
        }
    }
}