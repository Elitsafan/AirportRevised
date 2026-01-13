using Airport.Contracts.EventArgs;
using Airport.Contracts.Helpers;
using Airport.Domain.EventArgs;
using Airport.Domain.Exceptions;
using Airport.Domain.Repositories;
using Airport.Models.DTOs;
using Airport.Models.Entities;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Services
{
    public class StationService : IStationService
    {
        #region Fields
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMapper _mapper;
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<StationService> _logger;
        #endregion

        public StationService(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            IDomainEvents domainEvents,
            ILogger<StationService> logger)
        {
            _repositoryManager = repositoryManager;
            _mapper = mapper;
            _domainEvents = domainEvents;
            _logger = logger;
        }

        public async IAsyncEnumerable<StationDTO> GetAllStationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stations = await _repositoryManager.StationRepository
                .GetAllAsync(cancellationToken);

            foreach (var station in stations.Select(_mapper.Map<StationDTO>))
                yield return station;
        }

        public async Task<StationDTO?> GetStationByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var station = await _repositoryManager.StationRepository
                    .GetStationByIdAsync(id, cancellationToken);

                return _mapper.Map<StationDTO>(station);
            }
            catch (EntityNotFoundException)
            {
                _logger.LogInformation($"Station id: {id} not found");
                return null;
            }
        }

        public async Task<ObjectId> SaveStationAsync(
            StationForCreationDTO stationForCreationDTO,
            CancellationToken cancellationToken = default)
        {
            if (stationForCreationDTO is null)
                throw new ArgumentNullException(nameof(stationForCreationDTO));

            var stationDto = _mapper.Map<StationDTO>(stationForCreationDTO);
            var stationSaved = await _repositoryManager.StationRepository
                .SaveStationAsync(_mapper.Map<Station>(stationDto), cancellationToken);
            await _domainEvents.RaiseStationCreatedAsync(new StationCreatedEventArgs(stationDto.StationId));
            return stationSaved.StationId;
        }

        public async Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            StationForUpdateDTO stationForUpdate,
            CancellationToken cancellationToken = default)
        {
            var modifiedStation = _mapper.Map<Station>(stationForUpdate);
            var updateResult =  await _repositoryManager.StationRepository
                .UpdateStationAsync(id, modifiedStation, cancellationToken);
            await _domainEvents.RaiseStationUpdatedAsync(new StationUpdatedEventArgs(id));
            return updateResult;
        }

        public async Task<bool> DeleteStationAsync(
            ObjectId stationId,
            CancellationToken cancellationToken = default)
        {
            var routesContainId = (await _repositoryManager.RouteRepository
                .GetRoutesContainStationAsync(stationId, cancellationToken))
                .ToList();
            if (routesContainId.Count > 0)
                throw new InvalidOperationException(
                    $"Station can't be removed for it exists on routes: {string.Join(", ", routesContainId)}");
            var result = await _repositoryManager.StationRepository
                .DeleteStationAsync(stationId, cancellationToken);
            if (!result)
                _logger.LogInformation($"Route with id: {stationId} not found");
            else
                await _domainEvents.RaiseStationDeletedAsync(new StationDeletedEventArgs(stationId));
            return result;
        }
    }
}
