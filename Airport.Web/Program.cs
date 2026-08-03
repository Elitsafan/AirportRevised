using Airport.Contracts.Helpers;
using Airport.Persistence;
using Airport.Presentation.Converters;
using Airport.Services.MappingConfigurations;
using Airport.SignalR;
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
            
            builder.Services.Configure<AirportDbConfiguration>(builder.Configuration.GetSection(nameof(AirportDbConfiguration)));
            
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen(options =>
            {
                // This tells Swagger to treat ObjectId as a simple string schema
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
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new ObjectIdConverter());
                    options.SerializerSettings.Converters.Add(new TimeSpanConverter());
                })
                .AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly);
            
            builder.Services.AddAirportServices(builder.Configuration);
            
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
                var autoMapperKey = builder.Configuration.GetSection(nameof(AutoMapper))["Key"];
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

            var app = builder.Build();

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var domainEvents = scope.ServiceProvider.GetRequiredService<IDomainEvents>();

                await domainEvents.RaiseSystemResetRequestedAsync();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Running on {Environment} environment",
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            }

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

            await app.RunAsync();
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