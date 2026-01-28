using Airport.Contracts.Providers;
using Airport.Domain.Exceptions;
using Airport.Domain.Helpers;
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
    public class RouteService : IRouteService
    {
        #region Fields
        private readonly IAirportStateProvider _airportStateProvider;
        private readonly IRepositoryManager _repositoryManager;
        private readonly ILogger<RouteService> _logger;
        private readonly IMapper _mapper;
        #endregion

        public RouteService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repositoryManager,
            IMapper mapper,
            ILogger<RouteService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repositoryManager = repositoryManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var routes = await _repositoryManager.RouteRepository
                .GetAllAsync(ct);

            foreach (var route in routes.Select(_mapper.Map<RouteDTO>))
                yield return route;
        }

        public async Task<RouteDTO?> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var route = await _repositoryManager.RouteRepository
                .GetRouteByIdAsync(id, ct);

            return _mapper.Map<RouteDTO>(route);
        }

        public async Task<RouteDTO> AddRouteAsync(
            RouteForCreationDTO routeForCreationDTO,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeForCreationDTO is null)
                throw new ArgumentNullException(nameof(routeForCreationDTO));
            await ValidateRouteAsync(routeForCreationDTO.Directions, ct);
            await AreAnyStationsBetweenTrafficLightsAsync(routeForCreationDTO.Directions);

            var route = _mapper.Map<Route>(routeForCreationDTO);
            route = await _repositoryManager.RouteRepository
                .AddOneAsync(route, ct);
            await AddRouteTrafficLightsAsync(route, ct);

            return _mapper.Map<RouteDTO>(route);
        }

        // TODO: tests for all UpdateResult values
        public async Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeForUpdate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeForUpdate is null)
                throw new ArgumentNullException(nameof(routeForUpdate));
            await ValidateRouteAsync(routeForUpdate.Directions, ct);
            await AreAnyStationsBetweenTrafficLightsAsync(routeForUpdate.Directions);

            var modifiedRoute = _mapper.Map<Route>(routeForUpdate);
            var updateResult = await _repositoryManager.RouteRepository
                .UpdateRouteAsync(id, modifiedRoute, ct);

            if (updateResult == UpdateResult.Modified)
                await UpdateTrafficLightsAsync(modifiedRoute, ct);
            return updateResult;
        }

        public async Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

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

        private async Task AreAnyStationsBetweenTrafficLightsAsync(IEnumerable<DirectionDTO> directions)
        {
            var existOnRoutes = new Dictionary<ObjectId, int>(directions
                .SelectMany(d => new[] { d.From, d.To })
                .GroupBy(
                    id => id,
                    (id, ids) => new KeyValuePair<ObjectId, int>(id, ids.Count())));
            foreach (var direction in directions)
            {
                if (!await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(direction.From))
                    continue;
                var fromCount = ++existOnRoutes[direction.From];
                if (await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(direction.To))
                {
                    var toCount = ++existOnRoutes[direction.To];
                    if (fromCount > 1 && toCount > 1)
                        throw new InvalidRouteStructureException(
                            "Route must have least one station that is not a traffic light" +
                            $"between two traffic lights:\n{direction.From}\n{direction.To}");
                }
            }
        }

        private async Task ValidateRouteAsync(IEnumerable<DirectionDTO> directions, CancellationToken ct)
        {
            IEnumerable<ObjectId> ids = await ValidateStationsExistenceAsync(directions, ct);
            ValidateIfCircularRoute(directions, ids);
        }

        private void ValidateIfCircularRoute(IEnumerable<DirectionDTO> directions, IEnumerable<ObjectId> ids)
        {
            var graph = new Graph<ObjectId>(ids);

            foreach (var direction in directions)
                graph.AddEdge(direction.From, direction.To);

            if (graph.IsCircular())
                throw new InvalidRouteStructureException("A circular route is forbidden.");
        }

        private async Task<IEnumerable<ObjectId>> ValidateStationsExistenceAsync(
            IEnumerable<DirectionDTO> directions,
            CancellationToken ct)
        {
            var froms = directions
                .Select(d => d.From)
                .Distinct();
            var tos = directions
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

        private async Task AddRouteTrafficLightsAsync(Route route, CancellationToken ct)
        {
            IEnumerable<ObjectId> stationIds = ExtractStationIds(route);
            var addedIds = new List<ObjectId>();
            foreach (var id in stationIds)
                if (await _repositoryManager.RouteRepository.IsExistOnAnyRoutesAsync(id, ct: ct))
                {
                    var tl = new TrafficLight { StationId = id };
                    await _repositoryManager.TrafficLightRepository.AddOneAsync(tl, ct);
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
                    await _repositoryManager.TrafficLightRepository.AddOneAsync(tl, ct);
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