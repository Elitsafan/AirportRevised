using Airport.Contracts.Factories;
using Airport.Contracts.Providers;
using Airport.Domain.Factories;
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
using MongoDB.Driver;

namespace Airport.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Configuration = builder.Configuration;
            builder.Services.Configure<AirportDbConfiguration>(
                Configuration.GetSection(nameof(AirportDbConfiguration)));
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
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR(/*options => options.EnableDetailedErrors = true*/);
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        var clientOrigins = Configuration
                            .GetSection("ClientOrigins")
                            .GetChildren()
                            .Select(cs => cs.Value)!
                            .ToArray()!;
                        builder.WithOrigins(clientOrigins!)
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
            builder.Services.AddScoped<IAirportService, AirportService>();
            builder.Services.AddSingleton<IStationLogicProvider>(serviceProvider =>
            {
                var cache = serviceProvider.GetRequiredService<IMemoryCache>();
                var logger = serviceProvider.GetRequiredService<ILogger<StationLogicProvider>>();
                return StationLogicProvider.CreateAsync(serviceProvider, cache, logger).Result;
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
                var connectionString = Configuration["AirportDbConfiguration__ConnectionString"]
                    ?? Configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Database connection string is missing");
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                settings.ConnectTimeout = TimeSpan.FromMinutes(1);
                settings.MaxConnectionPoolSize = 25;
                settings.MinConnectionPoolSize = 5;
                return new MongoClient(settings);
            });
            builder.Services.AddAutoMapper(cfg =>
            {
                var autoMapperKey = Configuration.GetSection("AutoMapper")["Key"];
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
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                await SeedDatabaseAsync(app);
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                await SeedDatabaseAsync(app);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapHub<AirportHub>("/airporthub");
            app.MapControllers();
            await app.StartAsync();
            await app.WaitForShutdownAsync();
        }

        private static ConfigurationManager Configuration { get; set; } = null!;

        private static async Task SeedDatabaseAsync(WebApplication app)
        {
            var client = app.Services.GetRequiredService<IMongoClient>();
            var dbConfiguration = app.Services.GetRequiredService<IOptions<AirportDbConfiguration>>();
            try
            {
                await SeedData.DeleteAsync(client, dbConfiguration);
                await SeedData.InitializeAsync(client, dbConfiguration);

                // Refresh the station logics cache after seeding
                var stationLogicProvider = app.Services.GetRequiredService<IStationLogicProvider>();
                if (stationLogicProvider is StationLogicProvider provider)
                {
                    await provider.RefreshAsync();
                }
            }
            catch (TimeoutException ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "A database seeding error occurred.");
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "A database seeding error occurred.");
            }
        }
    }
}