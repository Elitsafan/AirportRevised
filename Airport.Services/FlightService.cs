//#define TEST
using Airport.Contracts.Factories;
using Airport.Contracts.Helpers;
using Airport.Contracts.Logics;
using Airport.Contracts.Providers;
#if TEST
using Airport.Domain.Helpers;
#endif
using Airport.Domain.Repositories;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Services
{
    public class FlightService : IFlightService
    {
        #region Fields
        private readonly IAirportStateProvider _airportStateProvider;
        private readonly IFlightLogicFactory _flightLogicFactory;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly ILogger<FlightService> _logger;
        private IFlightLogic _flightLogic = null!;
        #endregion

        public FlightService(
            IAirportStateProvider airportStateProvider,
            IFlightLogicFactory flightLogicFactory,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            ILogger<FlightService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _flightLogicFactory = flightLogicFactory;
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<FlightDTO> AddFlightAsync(FlightForCreationDTO flightToCreate, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (flightToCreate is null)
                throw new ArgumentNullException(nameof(flightToCreate));

            ct.ThrowIfCancellationRequested();
            Flight flight = _mapper.Map<Flight>(flightToCreate);
            flight.FlightId = ObjectId.GenerateNewId(DateTime.Now);
            await RunFlightAsync(flight, ct);
            return _mapper.Map<FlightDTO>(flight);
        }

        public async IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var flights = minutesPassed.HasValue
                ? (await _repositoryManager.FlightRepository.FilterByTimePassedAsync(
                    TimeSpan.FromMinutes(minutesPassed.Value),
                    ct))
                : (await _repositoryManager.FlightRepository.OrderByEntranceAsync(ct));

            foreach (var flight in flights.Select(_mapper.Map<FlightDTO>))
                yield return flight;
        }

        public async Task<FlightDTO> GetFlightByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var flight = await _repositoryManager.FlightRepository.GetByIdAsync(id, ct);

            return _mapper.Map<FlightDTO>(flight);
        }

        public async Task<bool> DeleteFlightAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var result = await _repositoryManager.FlightRepository.DeleteOneAsync(id, ct);
            if (!result)
                _logger.LogInformation($"Flight with id: {id} not found");
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            if (_flightLogic != null)
                await _flightLogic.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        private async Task RunFlightAsync(Flight flight, CancellationToken ct = default)
        {
            _flightLogic = (await _flightLogicFactory
                .GetCreatorAsync(flight, ct))
                .Create();
            using var cts = new CancellationTokenSource();
            await _flightLogic.RunAsync(cts.Token);
            await _repositoryManager.FlightRepository.UpdateFlightAsync(flight, ct: cts.Token);
#if TEST
            _logger.LogInformation($"{_flightLogic.FlightType} ID: {_flightLogic.FlightId} -----> Unegistered");
#endif
            await _flightLogic.RaiseFlightRunDoneAsync(cts.Token);
        }
    }
}
