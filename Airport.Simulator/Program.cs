#define TEST
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Airport.Simulator.Abstractions;
using Airport.Simulator.Configurations;
using Airport.Simulator.Services;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Airport.Simulator
{
    public class Program
    {
        private static ILogger<Program> _logger = null!;
        private static IConfiguration Configuration { get; set; } = null!;

        public static async Task Main(params string[] args)
        {
            // Global exception handling
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            using var host = Host
                .CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(AppContext.BaseDirectory).AddJsonFile(
                        "appsettings.json",
                        optional: false,
                        reloadOnChange: true);
                    Configuration = config.Build();
                })
                .ConfigureServices(hostingContext =>
                {
                    // Http client
                    hostingContext.AddHttpClient<IFlightLauncherService, FlightLauncherService>()
                        .AddPolicyHandler(GetRetryPolicy());
                    hostingContext.AddScoped<IFlightGenerator, FlightGenerator>();
                    hostingContext.Configure<FlightEndPointsConfiguration>(
                        Configuration.GetSection(nameof(FlightEndPointsConfiguration)));
                    hostingContext.Configure<FlightTimeoutConfiguration>(
                        Configuration.GetSection(nameof(FlightTimeoutConfiguration)));
                    hostingContext.AddSingleton<IFlightEndPointsConfiguration>(
                        provider => provider.GetRequiredService<IOptions<FlightEndPointsConfiguration>>().Value);
                    hostingContext.AddSingleton<IFlightTimeoutConfiguration>(
                        provider => provider.GetRequiredService<IOptions<FlightTimeoutConfiguration>>().Value);
                })
                .Build();

            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            IFlightLauncherService flightLauncherService = host.Services.GetRequiredService<IFlightLauncherService>();
            var startResponse = await flightLauncherService.StartAsync();
#if !TEST
            await Console.Out.WriteLineAsync(startResponse.StatusCode.ToString());
            await flightLauncherService
                .LaunchManyAsync(args)
                .ToListAsync();
#else
            await flightLauncherService.SetFlightTimeoutAsync(/*Models.Enums.FlightType.Landing*/);
#endif
            await host.RunAsync();
        }

        // Adds Polly's policy for Http Retries with exponential backoff
        private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

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