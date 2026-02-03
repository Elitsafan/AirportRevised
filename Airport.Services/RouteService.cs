using Airport.Contracts.Helpers;
using Airport.Contracts.Providers;
using Airport.Domain.EventArgs.RouteEventArgs;
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
        private readonly IDomainEvents _domainEvents;
        private readonly ILogger<RouteService> _logger;
        private readonly IMapper _mapper;
        #endregion

        public RouteService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repositoryManager,
            IDomainEvents domainEvents,
            IMapper mapper,
            ILogger<RouteService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repositoryManager = repositoryManager;
            _domainEvents = domainEvents;
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

        public async Task<RouteDTO> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var route = await _repositoryManager.RouteRepository.GetByIdAsync(id, ct);

            return _mapper.Map<RouteDTO>(route);
        }

        public async Task<RouteDTO> AddRouteAsync(RouteForCreationDTO routeToCreate, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeToCreate is null)
                throw new ArgumentNullException(nameof(routeToCreate));
            await ValidateRouteAsync(routeToCreate.Directions, ct);
            await AreAnyStationsBetweenTrafficLightsAsync(routeToCreate.Directions, ct);

            var route = await _repositoryManager.RouteRepository
                .AddOneAsync(_mapper.Map<Route>(routeToCreate), ct);
            await AddRouteTrafficLightsAsync(route, ct);
            await _domainEvents.RaiseRouteCreatedAsync(new RouteCreatedEventArgs
            {
                RouteId = route.RouteId,
                RouteName = route.RouteName,
                Directions = route.Directions,
            });

            return _mapper.Map<RouteDTO>(route);
        }

        // TODO: tests for all UpdateResult values
        public async Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeToUpdate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeToUpdate is null)
                throw new ArgumentNullException(nameof(routeToUpdate));
            await ValidateRouteAsync(routeToUpdate.Directions, ct);
            await AreAnyStationsBetweenTrafficLightsAsync(routeToUpdate.Directions, ct);
            var oldRoute = await _repositoryManager.RouteRepository
                .GetByIdAsync(id, ct);

            var oldTrafficLights = (await _repositoryManager.TrafficLightRepository
                .GetTrafficLightsByRouteIdAsync(id))
                .ToList();
            var modifiedRoute = _mapper.Map<Route>(routeToUpdate);
            modifiedRoute.RouteId = id;
            var updateResult = await _repositoryManager.RouteRepository
                .UpdateRouteAsync(modifiedRoute, ct: ct);

            if (updateResult == UpdateResult.Modified)
            {
                await UpdateTrafficLightsAsync(oldTrafficLights, modifiedRoute, ct);
                await _domainEvents.RaiseRouteUpdatedAsync(new RouteUpdatedEventArgs
                {
                    RouteId = modifiedRoute.RouteId,
                    RouteName = modifiedRoute.RouteName,
                    Directions = modifiedRoute.Directions,
                    OldRoute = oldRoute
                });
            }
            return updateResult;
        }

        public async Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var route = await _repositoryManager.RouteRepository.GetByIdAsync(id);
            if (route is null)
            {
                _logger.LogInformation($"Route with id: {id} not found.");
                return false;
            }
            var trafficLights = (await _repositoryManager.TrafficLightRepository
                .GetTrafficLightsByRouteIdAsync(route.RouteId))
                .ToList();
            var result = await _repositoryManager.RouteRepository.DeleteOneAsync(id, ct);
            if (result)
            {
                await DeleteTrafficLightsAsync(trafficLights, ct);
                await _domainEvents.RaiseRouteDeletedAsync(new RouteDeletedEventArgs
                {
                    RouteId = route.RouteId,
                    RouteName = route.RouteName,
                    Directions = route.Directions
                });
            }
            return result;
        }

        private async Task AreAnyStationsBetweenTrafficLightsAsync(
            List<DirectionDTO> directions,
            CancellationToken ct = default)
        {
            var stationIds = directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();
            var commonStationIds = await _repositoryManager.StationRepository
                .GetCommonStationIdsWithCountsAsync(stationIds, ct);

            foreach (var direction in directions)
                if (commonStationIds.ContainsKey(direction.From) &&
                    commonStationIds.ContainsKey(direction.To))
                    throw new InvalidRouteStructureException(
                        "Route must have least one station that is not a traffic light " +
                        $"between two traffic lights:\n{direction.From}\n{direction.To}");
        }

        private async Task ValidateRouteAsync(
            List<DirectionDTO> directions,
            CancellationToken ct = default)
        {
            List<ObjectId> ids = (await ValidateStationsExistenceAsync(directions, ct)).ToList();
            ValidateIfCircularRoute(directions, ids);
        }

        private void ValidateIfCircularRoute(List<DirectionDTO> directions, IEnumerable<ObjectId> ids)
        {
            var graph = new Graph<ObjectId>(ids);

            foreach (var direction in directions)
                graph.AddEdge(direction.From, direction.To);

            if (graph.IsCircular())
                throw new InvalidRouteStructureException("A circular route is forbidden.");
        }

        private async Task<IEnumerable<ObjectId>> ValidateStationsExistenceAsync(
            List<DirectionDTO> directions,
            CancellationToken ct = default)
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

        private async Task AddRouteTrafficLightsAsync(Route route, CancellationToken ct = default)
        {
            var stationIds = route.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();
            var commonStationIds = await _repositoryManager.StationRepository
                .GetCommonStationIdsWithCountsAsync(stationIds, ct);
            foreach (var idEntry in commonStationIds)
            {
                if (idEntry.Value > 2)
                    continue;
                var tl = new TrafficLight { StationId = idEntry.Key };
                tl = await _repositoryManager.TrafficLightRepository.AddOneAsync(tl, ct);
                _logger.LogInformation($"New traffic light refering Station Id: {tl.StationId}");
            }
        }

        private async Task UpdateTrafficLightsAsync(
            IEnumerable<TrafficLight> oldTrafficLights,
            Route newRoute,
            CancellationToken ct = default)
        {
            await DeleteTrafficLightsAsync(oldTrafficLights.ToList(), ct);
            await AddRouteTrafficLightsAsync(newRoute);
        }

        private async Task DeleteTrafficLightsAsync(
            IEnumerable<TrafficLight> trafficLights,
            CancellationToken ct = default)
        {
            var commonStationIds = await _repositoryManager.StationRepository
                .GetCommonStationIdsWithCountsAsync(trafficLights.Select(
                    tl => tl.StationId).ToList(), ct);
            foreach (var tl in trafficLights)
            {
                if (commonStationIds[tl.StationId] > 2)
                    continue;
                if (await _repositoryManager.TrafficLightRepository
                    .DeleteOneAsync(tl.TrafficLightId, ct))
                    _logger.LogInformation($"Traffic light with Station Id: {tl.StationId} deleted.");
            }
        }
    }
}
