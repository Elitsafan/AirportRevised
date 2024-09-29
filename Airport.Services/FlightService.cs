//#define TEST
using Airport.Contracts.EventArgs;
using Airport.Contracts.Factories;
using Airport.Contracts.Logics;
using Airport.Contracts.Repositories;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Services.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Services
{
    public class FlightService : IFlightService
    {
        #region Fields
        private readonly IFlightLogicFactory _flightLogicFactory;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IAirportHubService _airportHubService;
        private readonly IMapper _mapper;
        private readonly ILogger<FlightService> _logger;
        private IFlightLogic? _flightLogic = null!;
        #endregion

        public FlightService(
            IFlightLogicFactory flightLogicFactory,
            IRepositoryManager repositoryManager,
            IAirportHubService airportHubService,
            IMapper mapper,
            ILogger<FlightService> logger)
        {
            _flightLogicFactory = flightLogicFactory;
            _repositoryManager = repositoryManager;
            _airportHubService = airportHubService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task ProcessFlightAsync(
            ObjectId id,
            FlightForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (flightForCreation is null)
                throw new ArgumentNullException(nameof(flightForCreation));
            Flight flight;
            using var cts = new CancellationTokenSource();
            flight = _mapper.Map<Flight>(flightForCreation);
            flight.FlightId = id;
            _flightLogic = await _flightLogicFactory
                .GetCreator(flight)
                .CreateAsync();
            _flightLogic.FlightRunStarted += OnFlightRunStartedAsync;
            _airportHubService.RegisterFlightRunDone(_flightLogic);
            // Starts the run
            await _flightLogic.RunAsync(cts.Token);
            try
            {
                // Updates flight after the run has ended
                await _repositoryManager.FlightRepository.UpdateFlightAsync(flight, cancellationToken: cts.Token);
            }
            catch (OperationCanceledException e)
            {
                throw new OperationCanceledException($"{flight.FlightId} Error while updating flight.", e);
            }
#if TEST
            _logger.LogInformation($"{_flightLogic.FlightType} ID: {_flightLogic.FlightId} -----> Unegistered");
#endif
            await _flightLogic.RaiseFlightRunDoneAsync(cts.Token);
        }

        public async IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var flights = minutesPassed.HasValue
                ? (await _repositoryManager.FlightRepository.FilterByTimePassedAsync(
                    TimeSpan.FromMinutes(minutesPassed.Value),
                    cancellationToken))
                : (await _repositoryManager.FlightRepository.OrderByEntranceAsync(cancellationToken));

            foreach (var flight in flights.Select(_mapper.Map<FlightDTO>))
                yield return flight;
        }

        public async ValueTask DisposeAsync()
        {
            if (_flightLogic != null)
                await _flightLogic.DisposeAsync();
            GC.SuppressFinalize(this);
            await ValueTask.CompletedTask;
        }

        private async Task OnFlightRunStartedAsync(object? sender, IFlightRunStartedEventArgs args)
        {
            (sender as IFlightLogic)!.FlightRunStarted -= OnFlightRunStartedAsync;
            args.Flight.RouteId = args.RouteId;
            _repositoryManager.FlightRepository.AddFlightAsync(args.Flight).Forget();
#if TEST
            _logger.LogInformation($"{args.Flight.ConvertToFlightType()} ID: {args.Flight.FlightId} -----> Registered");
#endif
            await Task.CompletedTask;
        }
    }
}
