using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.Domain.EventArgs;
using Airport.Domain.Repositories;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Services
{
    public class StationService : IStationService
    {
        #region Fields
        private readonly IAirportStateProvider _airportStateProvider;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationService> _logger;
        #endregion

        public StationService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IDomainEvents domainEvents,
            ILogger<StationService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public async IAsyncEnumerable<StationDTO> GetAllStationsAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var stations = await _repositoryManager.StationRepository
                .GetAllAsync(ct);

            foreach (var station in stations.Select(_mapper.Map<StationDTO>))
                yield return station;
        }

        public async Task<StationDTO?> GetStationByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var station = await _repositoryManager.StationRepository
                .GetStationByIdAsync(id, ct);

            return _mapper.Map<StationDTO>(station);
        }

        public async Task<StationDTO> AddStationAsync(
            StationForCreationDTO stationForCreationDTO,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (stationForCreationDTO is null)
                throw new ArgumentNullException(nameof(stationForCreationDTO));
            var station = _mapper.Map<Station>(stationForCreationDTO);
            station = await _repositoryManager.StationRepository
                .AddOneAsync(station, ct);

            await _domainEvents.RaiseStationCreatedAsync(
                new StationCreatedEventArgs { StationId = station.StationId });

            return _mapper.Map<StationDTO>(station);
        }

        public async Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            StationForUpdateDTO stationForUpdate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (stationForUpdate is null)
                throw new ArgumentNullException(nameof(stationForUpdate));
            var modifiedStation = _mapper.Map<Station>(stationForUpdate);
            var updateResult = await _repositoryManager.StationRepository
                .UpdateStationAsync(id, modifiedStation, ct);
            await _domainEvents.RaiseStationUpdatedAsync(
                new StationUpdatedEventArgs { StationId = id });
            return updateResult;
        }

        public async Task<bool> DeleteStationAsync(ObjectId stationId, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var routesContainId = (await _repositoryManager.RouteRepository
                .GetRoutesContainStationAsync(stationId, ct))
                .ToList();
            if (routesContainId.Count > 0)
                throw new InvalidOperationException(
                    $"Station can't be removed for it exists on routes: {string.Join(", ", routesContainId)}");
            var result = await _repositoryManager.StationRepository
                .DeleteOneAsync(stationId, ct);
            if (!result)
                _logger.LogInformation($"Route with id: {stationId} not found");
            else
                await _domainEvents.RaiseStationDeletedAsync(
                    new StationDeletedEventArgs { StationId = stationId });
            return result;
        }
    }
}
