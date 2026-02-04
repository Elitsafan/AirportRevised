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
                    // Background sevice to keep target alive
                    config.AddHostedService<KeepAliveService>();
                    config.AddHttpClient(nameof(KeepAliveService))
                        .AddPolicyHandler(GetRetryPolicy());
                    // Http client
                    config.AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .AddPolicyHandler(GetRetryPolicy());
                    config.AddScoped<IFlightGenerator, FlightGenerator>();
                    config.Configure<FlightEndPointsConfiguration>(
                        hostingContext.Configuration.GetSection(nameof(FlightEndPointsConfiguration)));
                    config.Configure<FlightTimeoutConfiguration>(
                        hostingContext.Configuration.GetSection(nameof(FlightTimeoutConfiguration)));
                })
                .Build();

            await using var scope = host.Services.CreateAsyncScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var flightLauncherService = scope
                .ServiceProvider
                .GetRequiredService<IFlightLauncherService>();
            _logger.LogInformation("Starting Airport Simulator...");
            var startResponse = await flightLauncherService.StartAsync();
            flightLauncherService
                .StartStandbyModeAsync()
                .Forget();
            _logger.LogInformation($"Start response received with status: {startResponse.StatusCode}");
#if TEST
            await Console.Out.WriteLineAsync(startResponse.StatusCode.ToString());
            flightLauncherService
                .LaunchManyAsync(args)
                .ToListAsync()
                .Forget();
#else
            flightLauncherService
                .SetFlightTimeoutAsync(/*Models.Enums.FlightType.Landing*/)
                .Forget();
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

        // Exception handler
        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            _logger.LogError(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to Exit");
            Console.ReadLine();
            Environment.Exit(0);
        }
    }
}