//#define TEST
using Airport.Simulator.Abstractions;
using Airport.Simulator.Configurations;
using Airport.Simulator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
#if TEST
using Microsoft.VisualStudio.Threading; 
#endif

namespace Airport.Simulator
{
    public class Program
    {
        private static ILogger<Program> _logger = null!;

        public static async Task Main(params string[] args)
        {
            // Global exception handling
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

            using var host = Host
                .CreateDefaultBuilder(args)
                .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config.SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

                    config.Build();
                })
                .ConfigureServices((hostingContext, config) =>
                {
                    config.AddTransient<AuthTokenHandler>();

                    // Background sevice to keep target alive
                    config.AddHostedService<KeepAliveService>();

                    config.AddHttpClient(nameof(KeepAliveService)).AddPolicyHandler(GetRetryPolicy());

                    config.AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .AddHttpMessageHandler<AuthTokenHandler>()
                        .AddPolicyHandler(GetRetryPolicy());

                    config.AddHttpClient<IAuthService, AuthService>().AddPolicyHandler(GetRetryPolicy());

                    config.AddScoped<IFlightGenerator, FlightGenerator>();

                    config.AddScoped<IAuthService, AuthService>();

                    config.Configure<FlightEndPointsConfiguration>(
                        hostingContext.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));

                    config.Configure<FlightTimeoutConfiguration>(
                        hostingContext.Configuration.GetSection(nameof(FlightTimeoutConfiguration)));

                    config.Configure<LoginCredentials>(
                        hostingContext.Configuration.GetSection(nameof(LoginCredentials)));

                    config.Configure<AuthEndpoints>(
                        hostingContext.Configuration.GetSection(nameof(AuthEndpoints)));
                })
                .Build();

            await using var scope = host.Services.CreateAsyncScope();

            var flightLauncherService = scope.ServiceProvider.GetRequiredService<IFlightLauncherService>();

            flightLauncherService.StartStandbyModeAsync().Forget();

            _logger = host.Services.GetRequiredService<ILogger<Program>>();

            _logger.LogInformation("Running on {Environment} environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
#if TEST
            await Console.Out.WriteLineAsync(startResponse.StatusCode.ToString());
            flightLauncherService
                .LaunchManyAsync(args)
                .ToListAsync()
                .Forget();
#else
            flightLauncherService.SetFlightTimeoutAsync(/*Models.Enums.FlightType.Landing*/).Forget();
#endif
            await host.RunAsync();
        }

        // Adds Polly's policy for Http Retries with exponential backoff
        private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(6, retryAttempt =>
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));

                Console.WriteLine($"Retry attempt {retryAttempt}, waiting {delay.TotalSeconds} seconds");

                return delay;
            });

        // Global unhandled exception handler
        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError("{Exception}", e.ExceptionObject.ToString());

            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();

            Environment.Exit(0);
        }
    }
}