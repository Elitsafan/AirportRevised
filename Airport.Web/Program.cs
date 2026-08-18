using Airport.Contracts.Helpers;
using Airport.Models.DTOs;
using Airport.Persistence;
using Airport.Presentation.Converters;
using Airport.Services.MappingConfigurations;
using Airport.SignalR;
using AutoMapper.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

namespace Airport.Web
{
    public class Program
    {
        private static readonly string _scheme = "Bearer";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<AirportDbConfiguration>(builder.Configuration.GetSection(nameof(AirportDbConfiguration)));
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
            builder.Services.Configure<LoginCredentials>(builder.Configuration.GetSection(nameof(LoginCredentials)));

            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Auth settings is missing");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                    };

                    // For SignalR auth
                    //options.Events = new JwtBearerEvents
                    //{
                    //    OnMessageReceived = context =>
                    //    {
                    //        var accessToken = context.Request.Query["access_token"];
                    //        var path = context.HttpContext.Request.Path;

                    //        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/airporthub"))
                    //            context.Token = accessToken;

                    //        return Task.CompletedTask;
                    //    }
                    //};
                });

            builder.Services.AddAuthorization();

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

                options.AddSecurityDefinition(_scheme, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = _scheme,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = _scheme
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddSignalR(/*options => options.EnableDetailedErrors = true*/);

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(corsBuilder =>
                {
                    var clientOrigins = builder.Configuration
                        .GetSection("ClientOrigins")
                        .GetChildren()
                        .Select(cs => cs.Value)!
                        .ToArray();

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

                // Mitigate DoS / StackOverflow vulnerability by setting a recursion limit
                cfg.Internal().ForAllMaps((typeMap, map) => map.MaxDepth(32));

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

            var domainEvents = app.Services.GetRequiredService<IDomainEvents>();

            await domainEvents.RaiseSystemResetRequestedAsync();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation(
                "Running on {Environment} environment",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                await SeedDatabaseAsync(app, domainEvents, logger);
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                // For Demo purpose
                await SeedDatabaseAsync(app, domainEvents, logger);

                app.UseHsts();
            }

            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<AirportHub>("/airporthub");
            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task SeedDatabaseAsync(WebApplication app, IDomainEvents domainEvents, ILogger<Program> logger)
        {
            var client = app.Services.GetRequiredService<IMongoClient>();

            var dbConfiguration = app.Services.GetRequiredService<IOptions<AirportDbConfiguration>>();

            try
            {
                await SeedData.DeleteAsync(client, dbConfiguration);
                await SeedData.InitializeAsync(client, dbConfiguration);

                // Refresh the station logics cache after seeding
                await domainEvents.RaiseDataRefreshedAsync();
            }
            catch (TimeoutException ex)
            {
                logger.LogError(ex, "A database seeding timeout occurred.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A database seeding error occurred.");
            }
        }
    }
}