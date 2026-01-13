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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var routes = await _repositoryManager.RouteRepository
                .GetAllAsync(cancellationToken);

            foreach (var route in routes.Select(_mapper.Map<RouteDTO>))
                yield return route;
        }

        public async Task<RouteDTO?> GetRouteByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var route = await _repositoryManager.RouteRepository
                    .GetRouteByIdAsync(id, cancellationToken);

                return _mapper.Map<RouteDTO>(route);
            }
            catch (EntityNotFoundException)
            {
                _logger.LogInformation($"Route id: {id} not found");
                return null;
            }
        }

        public async Task<ObjectId> SaveRouteAsync(
            RouteForCreationDTO routeForCreationDTO,
            CancellationToken cancellationToken = default)
        {
            if (routeForCreationDTO is null)
                throw new ArgumentNullException(nameof(routeForCreationDTO));

            IEnumerable<ObjectId> ids = await ValidateRouteStationsExistenceAsync(routeForCreationDTO, cancellationToken);

            ValidateIfRouteCircular(routeForCreationDTO, ids);

            var routeDto = _mapper.Map<RouteDTO>(routeForCreationDTO);
            var routeSaved = await _repositoryManager.RouteRepository
                .SaveRouteAsync(_mapper.Map<Route>(routeDto), cancellationToken);
            return routeSaved.RouteId;
        }

        // TODO: tests for all UpdateResult values
        public async Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeForUpdate,
            CancellationToken cancellationToken = default)
        {
            await ValidateRouteAsync(routeForUpdate, cancellationToken);

            var modifiedRoute = _mapper.Map<Route>(routeForUpdate);
            return await _repositoryManager.RouteRepository
                .UpdateRouteAsync(id, modifiedRoute, cancellationToken);
        }

        public async Task<bool> DeleteRouteAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var result = await _repositoryManager.RouteRepository.DeleteRouteAsync(id, cancellationToken);
            if (!result)
                _logger.LogInformation($"Route with id: {id} not found");
            return result;
        }

        private async Task ValidateRouteAsync(RouteForOperationDTO route, CancellationToken cancellationToken)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));
            IEnumerable<ObjectId> ids = await ValidateRouteStationsExistenceAsync(route, cancellationToken);
            ValidateIfRouteCircular(route, ids);
        }

        private static void ValidateIfRouteCircular(
            RouteForOperationDTO route,
            IEnumerable<ObjectId> ids)
        {
            var graph = new Graph(ids);

            foreach (var direction in route.Directions)
                graph.AddEdge(direction.From, direction.To);
            if (graph.IsCircular())
                throw new InvalidRouteStructureException("Route is a circular route.");
        }

        private async Task<IEnumerable<ObjectId>> ValidateRouteStationsExistenceAsync(
            RouteForOperationDTO route,
            CancellationToken cancellationToken)
        {
            var froms = route.Directions
                .Select(d => d.From)
                .Distinct();
            var tos = route.Directions
                .Select(d => d.To)
                .Distinct();
            var ids = froms.Union(tos);

            var existingStationIds = await _repositoryManager.StationRepository
                .GetExistingStationIdsAsync(ids, cancellationToken);
            var missingIds = ids
                .Except(existingStationIds)
                .ToList();
            if (missingIds.Count != 0)
                throw new MissingRouteStationsException(
                    $"Stations don't exist: {string.Join(", ", missingIds)}. " +
                    "First insert the stations of the route, and then save the route.");
            return ids;
        }
    }
}
