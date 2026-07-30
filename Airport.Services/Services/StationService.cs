using Airport.Domain.EventArgs.StationEventArgs;
using Airport.Domain.Exceptions;
using Airport.Models.Enums;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using System.Runtime.CompilerServices;

namespace Airport.Services.Services
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

        public async Task<StationDTO> GetStationByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var station = await _repositoryManager.StationRepository.GetByIdAsync(id, ct);

            return _mapper.Map<StationDTO>(station);
        }

        public async Task<StationDTO> AddStationAsync(
            StationForCreationDTO stationToCreate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (stationToCreate is null)
                throw new ArgumentNullException(nameof(stationToCreate));

            var station = await _repositoryManager.StationRepository
                .AddOneAsync(_mapper.Map<Station>(stationToCreate), ct: ct);

            await _domainEvents.RaiseStationCreatedAsync(new StationCreatedEventArgs
            {
                StationId = station.StationId
            });

            return _mapper.Map<StationDTO>(station);
        }

        public async Task<UpdateResult> UpdateStationAsync(
            ObjectId id,
            StationForUpdateDTO stationToUpdate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (stationToUpdate is null)
                throw new ArgumentNullException(nameof(stationToUpdate));

            var modifiedStation = _mapper.Map<Station>(stationToUpdate);
            modifiedStation.StationId = id;

            var updateResult = await _repositoryManager.StationRepository
                .UpdateStationAsync(modifiedStation, ct: ct);

            await _domainEvents.RaiseStationUpdatedAsync(new StationUpdatedEventArgs
            {
                StationId = id
            });

            return updateResult;
        }

        public async Task<bool> DeleteStationAsync(ObjectId stationId, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var routesContainIds = (await _repositoryManager.RouteRepository
                .GetRoutesContainStationAsync(stationId, ct))
                .Select(r => new
                {
                    r.RouteId,
                    r.RouteName
                })
                .ToList();

            if (routesContainIds.Count > 0)
                throw new InvalidDeletionException(
                    $"Station can't be removed for it exists on routes:\n" +
                    $"{string.Join($",{Environment.NewLine}", routesContainIds)}");

            var result = await _repositoryManager.StationRepository.DeleteOneAsync(stationId, ct: ct);

            if (!result)
                _logger.LogInformation("Route with id: {id} not found.", stationId);
            else
                await _domainEvents.RaiseStationDeletedAsync(new StationDeletedEventArgs
                {
                    StationId = stationId
                });

            return result;
        }
    }
}
