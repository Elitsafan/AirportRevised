using Airport.Domain.Exceptions;
using Airport.Domain.Helpers;
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
    public class RouteService : IRouteService
    {
        #region Fields
        private readonly IRepositoryManager _repositoryManager;
        private readonly ILogger<RouteService> _logger;
        private readonly IMapper _mapper;
        #endregion

        public RouteService(
            IRepositoryManager repositoryManager,
            IMapper mapper,
            ILogger<RouteService> logger)
        {
            _repositoryManager = repositoryManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var routes = await _repositoryManager.RouteRepository
                .GetAllAsync(ct);

            foreach (var route in routes.Select(_mapper.Map<RouteDTO>))
                yield return route;
        }

        public async Task<RouteDTO?> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            try
            {
                var route = await _repositoryManager.RouteRepository
                    .GetRouteByIdAsync(id, ct);

                return _mapper.Map<RouteDTO>(route);
            }
            catch (EntityNotFoundException e)
            {
                _logger.LogError($"Route id: {id} not found", e);
                return null;
            }
        }

        public async Task<ObjectId> AddRouteAsync(RouteForCreationDTO routeForCreationDTO, CancellationToken ct = default)
        {
            await ValidateRouteAsync(routeForCreationDTO, ct);

            var routeDto = _mapper.Map<RouteDTO>(routeForCreationDTO);
            var routeSaved = await _repositoryManager.RouteRepository
                .AddRouteAsync(_mapper.Map<Route>(routeDto), ct);

            await AddTrafficLightsAsync(routeSaved, ct);

            return routeSaved.RouteId;
        }

        // TODO: tests for all UpdateResult values
        public async Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeForUpdate,
            CancellationToken ct = default)
        {
            await ValidateRouteAsync(routeForUpdate, ct);

            var modifiedRoute = _mapper.Map<Route>(routeForUpdate);
            var updateResult = await _repositoryManager.RouteRepository
                .UpdateRouteAsync(id, modifiedRoute, ct);

            if (updateResult == UpdateResult.Modified)
                await UpdateTrafficLightsAsync(modifiedRoute, ct);
            return updateResult;
        }

        public async Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken ct = default)
        {
            var route = await _repositoryManager.RouteRepository.GetRouteByIdAsync(id);
            if (route is null)
            {
                _logger.LogInformation($"Route with id: {id} not found.");
                return false;
            }
            var result = await _repositoryManager.RouteRepository.DeleteOneAsync(id, ct);
            if (result)
                await DeleteTrafficLightsAsync(route, ct);
            return result;
        }

        private async Task ValidateRouteAsync(RouteForOperationDTO route, CancellationToken ct)
        {
            IEnumerable<ObjectId> ids = await ValidateStationsExistenceAsync(route, ct);
            ValidateIfRouteCircular(route, ids);
        }

        private static void ValidateIfRouteCircular(
            RouteForOperationDTO route,
            IEnumerable<ObjectId> ids)
        {
            var graph = new Graph<ObjectId>(ids);

            foreach (var direction in route.Directions)
                graph.AddEdge(direction.From, direction.To);
            if (graph.IsCircular())
                throw new InvalidRouteStructureException("Route is a circular route.");
        }

        private async Task<IEnumerable<ObjectId>> ValidateStationsExistenceAsync(
            RouteForOperationDTO route,
            CancellationToken ct)
        {
            var froms = route.Directions
                .Select(d => d.From)
                .Distinct();
            var tos = route.Directions
                .Select(d => d.To)
                .Distinct();
            var ids = froms.Union(tos);

            var existingStationIds = await _repositoryManager.StationRepository
                .GetExistingStationIdsAsync(ids, ct);
            var missingIds = ids
                .Except(existingStationIds)
                .ToList();
            if (missingIds.Count != 0)
                throw new MissingRouteStationsException(
                    $"Stations don't exist:\n{string.Join(",\n", missingIds)}.\n" +
                    "First insert the stations of the route, and then save the route.");
            return ids;
        }

        private async Task AddTrafficLightsAsync(Route route, CancellationToken ct)
        {
            IEnumerable<ObjectId> stationIds = ExtractStationIds(route);
            var addedIds = new List<ObjectId>();
            foreach (var id in stationIds)
                if (await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(id, ct: ct))
                {
                    var tl = new TrafficLight { StationId = id };
                    await _repositoryManager.TrafficLightRepository.AddTrafficLightAsync(tl, ct);
                    addedIds.Add(id);
                }
            if (addedIds.Count > 0)
                _logger.LogInformation($"Traffic lights added:\n{string.Join(",\n", addedIds)}");
        }

        private async Task DeleteTrafficLightsAsync(Route route, CancellationToken ct)
        {
            IEnumerable<ObjectId> stationIds = ExtractStationIds(route);
            var deletedIds = new List<ObjectId>();
            foreach (var id in stationIds)
                if (!await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(id, 2, ct))
                {
                    await _repositoryManager.TrafficLightRepository.DeleteOneAsync(id, ct);
                    deletedIds.Add(id);
                }
            if (deletedIds.Count > 0)
                _logger.LogInformation($"Traffic lights deleted:\n{string.Join(",\n", deletedIds)}");
        }

        private async Task UpdateTrafficLightsAsync(Route route, CancellationToken ct)
        {
            IEnumerable<ObjectId> stationIds = ExtractStationIds(route);
            var updatedIds = new List<ObjectId>();
            var deletedIds = new List<ObjectId>();
            foreach (var id in stationIds)
                if (await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(id, 2, ct))
                {
                    var tl = new TrafficLight { StationId = id };
                    await _repositoryManager.TrafficLightRepository.AddTrafficLightAsync(tl, ct);
                    updatedIds.Add(id);
                }
                else
                {
                    await _repositoryManager.TrafficLightRepository.DeleteOneAsync(id, ct);
                    deletedIds.Add(id);
                }

            if (updatedIds.Count > 0)
                _logger.LogInformation($"Traffic lights updated:\n{string.Join(",\n", updatedIds)}");
            if (deletedIds.Count > 0)
                _logger.LogInformation($"Traffic lights deleted:\n{string.Join(",\n", deletedIds)}");
        }

        private IEnumerable<ObjectId> ExtractStationIds(Route route) => route.Directions
            .Select(d => d.From)
            .Distinct()
            .Union(route.Directions
                .Select(d => d.To)
                .Distinct())
            .ToList();
    }
}
