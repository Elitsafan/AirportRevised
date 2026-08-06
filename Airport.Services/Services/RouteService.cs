using Airport.Domain.EventArgs.RouteEventArgs;
using Airport.Domain.EventArgs.SectionEventArgs;
using Airport.Domain.EventArgs.SyncerEventArgs;
using Airport.Services.Abstractions;
using Airport.Services.Extensions;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using UpdateResult = Airport.Models.Enums.UpdateResult;

namespace Airport.Services.Services
{
    public class RouteService : IRouteService
    {
        #region Fields
        private readonly IAirportStateProvider _airportStateProvider;
        private readonly IRepositoryManager _repoManager;
        private readonly IDomainEvents _domainEvents;
        private readonly IMapper _mapper;
        private readonly IMongoClient _client;
        private readonly IRouteValidator _routeValidator;
        private readonly ILogger<RouteService> _logger;
        #endregion

        public RouteService(
            IAirportStateProvider airportStateProvider,
            IRepositoryManager repositoryManager,
            IDomainEvents domainEvents,
            IMapper mapper,
            IMongoClient client,
            IRouteValidator routeValidator,
            ILogger<RouteService> logger)
        {
            _airportStateProvider = airportStateProvider;
            _repoManager = repositoryManager;
            _domainEvents = domainEvents;
            _mapper = mapper;
            _client = client;
            _routeValidator = routeValidator;
            _logger = logger;
        }

        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var routes = await _repoManager.RouteRepository
                .GetAllAsync(ct);

            foreach (var route in routes.Select(_mapper.Map<RouteDTO>))
                yield return route;
        }

        public async Task<RouteDTO> GetRouteByIdAsync(ObjectId routeId, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var route = await _repoManager.RouteRepository.GetByIdAsync(routeId, ct);

            return _mapper.Map<RouteDTO>(route);
        }

        public async Task<RouteDTO> AddRouteAsync(RouteForCreationDTO routeToCreate, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeToCreate is null)
                throw new ArgumentNullException(nameof(routeToCreate));

            var ids = routeToCreate.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();

            // Get the "current" common stations excluding the route for creation.
            // Meaning, get the common stations before the new route creation,
            // and using those new route stations.
            var comStationIds = await _repoManager.StationRepository.GetCommonIdsToCountsAsync(ids, null, 0, ct);

            var newSectionDtos = await _routeValidator.ValidateRouteAsync(routeToCreate.Directions, comStationIds, ct);

            var newSections = _mapper.Map<List<Section>>(newSectionDtos);

            Route route;
            List<Route>? affectedRoutes = null;
            List<ObjectId> syncerIds = new();
            List<ObjectId> deletedSyncerIds = new();
            List<ObjectId>[]? updatedSectionIds = null;

            using var session = await _client.StartSessionAsync(cancellationToken: ct);

            session.StartTransaction();

            try
            {
                // Handle new route
                route = _mapper.Map<Route>(routeToCreate);

                route = await _repoManager.RouteRepository.AddOneAsync(route, session, ct);

                if (comStationIds.Count > 0)
                {
                    await ProcessSyncersAndSectionsAsync(route.RouteId, syncerIds, newSections, session, ct);

                    (updatedSectionIds, affectedRoutes) = await HandleAffectedRoutesAsync(
                        route,
                        syncerIds,
                        deletedSyncerIds,
                        updatedSectionIds,
                        comStationIds,
                        session,
                        ct);

                    var candidates = comStationIds
                        .Where(kvp => kvp.Value == 1)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    await AddTrafficLightsAsync(candidates, session, ct);
                }
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync(ct);
                throw;
            }

            await session.CommitTransactionAsync(ct);

            if (comStationIds.Count > 0)
            {
                // Syncers deleted event
                await _domainEvents.RaiseSyncersDeletedAsync(new SyncersDeletedEventArgs
                {
                    SyncerIds = deletedSyncerIds,
                });

                // Syncers updated event
                await _domainEvents.RaiseSyncersUpdatedAsync(new SyncersUpdatedEventArgs
                {
                    SyncerIds = syncerIds
                });

                // New sections created event
                await _domainEvents.RaiseSectionsCreatedAsync(new SectionsCreatedEventArgs
                {
                    RouteId = route.RouteId,
                    SectionIds = newSections.Select(s => s.SectionId).ToList()
                });
            }

            // New route created event
            await _domainEvents.RaiseRouteCreatedAsync(new RouteCreatedEventArgs
            {
                RouteId = route.RouteId,
                StandaloneTLIds = (await _repoManager.TrafficLightRepository
                    .GetStandaloneTLsAsync(route.RouteId, ct))
                    .Select(tl => tl.StationId)
                    .ToList()
            });

            // Affected routes and sections updated events
            for (int i = 0; i < affectedRoutes?.Count; i++)
            {
                await _domainEvents.RaiseSectionsDeletedAsync(new SectionsDeletedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                });

                await _domainEvents.RaiseRouteUpdatedAsync(new RouteUpdatedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                    StandaloneTLIds = (await _repoManager.TrafficLightRepository
                        .GetStandaloneTLsAsync(route.RouteId, ct))
                        .Select(tl => tl.StationId)
                        .ToList()
                });
            }

            return _mapper.Map<RouteDTO>(route);
        }

        // TODO: fix if (comStationIds.Count > 0)
        // TODO: tests for all UpdateResult values
        public async Task<UpdateResult> UpdateRouteAsync(
            ObjectId routeId,
            RouteForUpdateDTO routeToUpdate,
            CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            if (routeToUpdate is null)
                throw new ArgumentNullException(nameof(routeToUpdate));

            var route = _mapper.Map<Route>(routeToUpdate);

            route.RouteId = routeId;

            UpdateResult updateResult;
            List<ObjectId> syncerIds = new();
            List<ObjectId> deletedSyncerIds = new();
            List<ObjectId>[]? updatedSectionIds = null;
            List<Route>? affectedRoutes = null;

            var oldStationIds = (await _repoManager.StationRepository.GetStationsByRouteIdAsync(routeId, ct))
                .Select(s => s.StationId)
                .ToList();

            var newStationIds = routeToUpdate.Directions
                .SelectMany(d => new[] { d.From, d.To })
                .Distinct()
                .ToList();

            var oldComStationIds = await _repoManager.StationRepository
                .GetCommonIdsToCountsAsync(oldStationIds, ct: ct);

            var newComStationIds = await _repoManager.StationRepository
                .GetCommonIdsToCountsAsync(newStationIds, new[] { routeId }, 0, ct: ct);

            var newSectionDtos = await _routeValidator.ValidateRouteAsync(routeToUpdate.Directions, newComStationIds, ct);

            var newSections = _mapper.Map<List<Section>>(newSectionDtos);

            using var session = await _client.StartSessionAsync(cancellationToken: ct);

            session.StartTransaction();

            try
            {
                updateResult = await _repoManager.RouteRepository.UpdateRouteAsync(route, session, ct: ct);

                if (updateResult == UpdateResult.Failed)
                    await session.AbortTransactionAsync(ct);

                // Update traffic lights
                if (oldComStationIds.Count > 0 || newComStationIds.Count > 0)
                {
                    var newTLCandidates = newComStationIds
                        .Where(kvp => kvp.Value == 1)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    List<ObjectId>? oldTLCandidates = null;

                    oldTLCandidates = oldComStationIds
                        .Where(kvp => kvp.Value == 2 && !newComStationIds.TryGetValue(kvp.Key, out var _))
                        .Select(kvp => kvp.Key)
                        .ToList();

                    await UpdateTrafficLightsAsync(oldTLCandidates, newTLCandidates, session, ct);

                    if (newTLCandidates is not null)
                        foreach (var id in newTLCandidates)
                            newComStationIds?.Remove(id);
                }

                // Delete old sections
                await DeleteSectionsAsync(routeId, session, ct);

                // Update syncers
                await _repoManager.SyncerRepository.UpdateAfterRemoveRouteIdAsync(routeId, session, ct);

                // Delete syncers with capacity == 0
                deletedSyncerIds.AddRange((await _repoManager.SyncerRepository
                    .DeleteIfChildlessAsync(session, ct))
                    .ToList());

                await ProcessSyncersAndSectionsAsync(routeId, syncerIds, newSections, session, ct);

                (updatedSectionIds, affectedRoutes) = await HandleAffectedRoutesAsync(
                    route,
                    syncerIds,
                    deletedSyncerIds,
                    updatedSectionIds,
                    newComStationIds,
                    session,
                    ct);

            }
            catch (Exception)
            {
                await session.AbortTransactionAsync(ct);

                throw;
            }

            await session.CommitTransactionAsync(ct);

            var affectedRouteIds = affectedRoutes.Select(s => s.RouteId).ToList();

            // Syncers deleted event
            await _domainEvents.RaiseSyncersDeletedAsync(new SyncersDeletedEventArgs
            {
                SyncerIds = deletedSyncerIds,
            });

            // Syncers updated event
            await _domainEvents.RaiseSyncersUpdatedAsync(new SyncersUpdatedEventArgs
            {
                SyncerIds = syncerIds
            });

            // Old route updated event
            await _domainEvents.RaiseRouteUpdatedAsync(new RouteUpdatedEventArgs
            {
                RouteId = route.RouteId,
                StandaloneTLIds = (await _repoManager.TrafficLightRepository
                    .GetStandaloneTLsAsync(route.RouteId, ct))
                    .Select(tl => tl.StationId)
                    .ToList()
            });

            // Old route sections deleted event
            await _domainEvents.RaiseSectionsDeletedAsync(new SectionsDeletedEventArgs
            {
                RouteId = route.RouteId,
            });

            // Affected routes and sections updated
            for (int i = 0; i < affectedRoutes.Count; i++)
            {
                await _domainEvents.RaiseSectionsDeletedAsync(new SectionsDeletedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                });

                if (updatedSectionIds?[i] is not null)
                    // New sections created event
                    await _domainEvents.RaiseSectionsCreatedAsync(new SectionsCreatedEventArgs
                    {
                        RouteId = affectedRoutes[i].RouteId,
                        SectionIds = updatedSectionIds[i]
                    });

                await _domainEvents.RaiseRouteUpdatedAsync(new RouteUpdatedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                    StandaloneTLIds = (await _repoManager.TrafficLightRepository
                        .GetStandaloneTLsAsync(route.RouteId, ct))
                        .Select(tl => tl.StationId)
                        .ToList()
                });
            }

            return updateResult;
        }

        public async Task<bool> DeleteRouteAsync(ObjectId routeId, CancellationToken ct = default)
        {
            _airportStateProvider.ThrowIfNotStarted();

            var route = await _repoManager.RouteRepository.GetByIdAsync(routeId, ct);

            if (route is null)
            {
                _logger.LogInformation("Route with id: {id} not found.", routeId);

                return false;
            }

            List<ObjectId> syncerIds = new();
            List<ObjectId> deletedSyncerIds = new();
            List<ObjectId>[]? updatedSectionIds = null;
            List<Route>? affectedRoutes = null;

            var stationIds = ExtractStationIds(route.Directions);

            var comStationIds = await _repoManager.StationRepository.GetCommonIdsToCountsAsync(stationIds, ct: ct);

            var oldSections = (await _repoManager.SectionRepository
                .GetByRouteIdAsync(routeId, ct))
                .ToList();

            using var session = await _client.StartSessionAsync(cancellationToken: ct);

            session.StartTransaction();

            try
            {
                // Delete the route
                var result = await _repoManager.RouteRepository.DeleteOneAsync(routeId, session, ct);

                // Delete old traffic lights
                if (comStationIds.Count > 0)
                {
                    // Filter the stations showed on exactly two routes BEFORE the route deletion
                    var candidates = comStationIds
                        .Where(kvp => kvp.Value == 2)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    // Change a traffic light (common station) to a regular (singular) station
                    await DeleteTrafficLightsAsync(candidates, session, ct);

                    // Remove deleted traffic lights
                    foreach (var candidate in candidates)
                        comStationIds.Remove(candidate);
                }

                // Delete old sections
                await DeleteSectionsAsync(routeId, session, ct);

                // Update syncers
                await _repoManager.SyncerRepository.UpdateAfterRemoveRouteIdAsync(routeId, session, ct);

                // Delete syncers with capacity == 0
                deletedSyncerIds.AddRange((await _repoManager.SyncerRepository
                    .DeleteIfChildlessAsync(session, ct))
                    .ToList());

                (updatedSectionIds, affectedRoutes) = await HandleAffectedRoutesAsync(
                    route,
                    syncerIds,
                    deletedSyncerIds,
                    updatedSectionIds,
                    comStationIds,
                    session,
                    ct);
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync(ct);
                throw;
            }

            await session.CommitTransactionAsync(ct);

            // Syncers deleted event
            await _domainEvents.RaiseSyncersDeletedAsync(new SyncersDeletedEventArgs
            {
                SyncerIds = deletedSyncerIds,
            });

            // Syncers updated event
            await _domainEvents.RaiseSyncersUpdatedAsync(new SyncersUpdatedEventArgs
            {
                SyncerIds = syncerIds
            });

            // Old route deleted event
            await _domainEvents.RaiseRouteDeletedAsync(new RouteDeletedEventArgs
            {
                RouteId = route.RouteId,
            });

            // Old route sections deleted event
            await _domainEvents.RaiseSectionsDeletedAsync(new SectionsDeletedEventArgs
            {
                RouteId = route.RouteId,
            });

            // Affected routes and sections updated
            for (int i = 0; i < affectedRoutes.Count; i++)
            {
                await _domainEvents.RaiseSectionsDeletedAsync(new SectionsDeletedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                });

                if (updatedSectionIds?[i] is not null)
                    // New sections created event
                    await _domainEvents.RaiseSectionsCreatedAsync(new SectionsCreatedEventArgs
                    {
                        RouteId = affectedRoutes[i].RouteId,
                        SectionIds = updatedSectionIds[i]
                    });

                await _domainEvents.RaiseRouteUpdatedAsync(new RouteUpdatedEventArgs
                {
                    RouteId = affectedRoutes[i].RouteId,
                    StandaloneTLIds = (await _repoManager.TrafficLightRepository
                        .GetStandaloneTLsAsync(route.RouteId, ct))
                        .Select(tl => tl.StationId)
                        .ToList()
                });
            }

            return true;
        }

        public void Dispose()
        {
        }

        private async Task<(List<ObjectId>[]? updatedSectionIds, List<Route> affectedRoutes)> HandleAffectedRoutesAsync(
            Route route,
            List<ObjectId> syncerIds,
            List<ObjectId> deletedSyncerIds,
            List<ObjectId>[]? updatedSectionIds,
            Dictionary<ObjectId, int>? comStationIds,
            IClientSessionHandle session,
            CancellationToken ct = default)
        {
            var affectedRoutes = (await _repoManager.RouteRepository
                .IntersectedRoutesAsync(route, ct))
                .ToList();

            // Holds ids of affected routes sections
            updatedSectionIds = new List<ObjectId>[affectedRoutes.Count];

            for (int i = 0; i < affectedRoutes.Count; i++)
            {
                // Delete old sections
                await DeleteSectionsAsync(affectedRoutes[i].RouteId, session, ct);

                // Update syncers
                await _repoManager.SyncerRepository.UpdateAfterRemoveRouteIdAsync(affectedRoutes[i].RouteId, session, ct);

                // Delete syncers with capacity == 0
                deletedSyncerIds.AddRange((await _repoManager.SyncerRepository
                    .DeleteIfChildlessAsync(session, ct))
                    .ToList());

                var sectionDtos = await _routeValidator.ValidateRouteAsync(_mapper.Map<List<DirectionDTO>>(affectedRoutes[i].Directions), comStationIds, ct);

                var sections = _mapper.Map<List<Section>>(sectionDtos);

                updatedSectionIds[i] = sections
                    .Select(s => s.SectionId)
                    .ToList();

                // Handle syncers and sections of affected routes
                await ProcessSyncersAndSectionsAsync(affectedRoutes[i].RouteId, syncerIds, sections!, session, ct);
            }

            return (updatedSectionIds, affectedRoutes);
        }

        private async Task ProcessSyncersAndSectionsAsync(
            ObjectId routeId,
            List<ObjectId> syncerIds,
            List<Section> sections,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            List<Syncer> newSyncers = new();
            List<Syncer> syncers = new();

            foreach (var section in sections)
            {
                var syncer = await _repoManager.SyncerRepository
                    .GetSyncerBySectionAsync(section, ct);

                if (syncer is not null)
                    syncers.Add(syncer);
                else
                {
                    syncer = new Syncer();

                    newSyncers.Add(syncer);
                }

                section.RouteId = routeId;
                section.SyncerId = syncer.SyncerId;

                syncer.SectionCriticalOccupations.Add(new SectionCriticalOccupation()
                {
                    RouteId = routeId,
                    Value = section.Origin.Count + section.SectionOnly.Count
                });
            }

            await _repoManager.SyncerRepository.AddManyAsync(newSyncers, session, ct);

            await _repoManager.SectionRepository.AddManyAsync(sections, session, ct);

            await UpdateSyncersAsync(newSyncers, syncers, session, ct);

            syncerIds = syncers.Select(s => s.SyncerId).ToList();
        }

        private async Task UpdateSyncersAsync(
            List<Syncer> newSyncers,
            List<Syncer> syncers,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            syncers.AddRange(newSyncers);

            var syncerIds = syncers.Select(s => s.SyncerId).ToList();

            var syncerIdToCapacity = await _repoManager.SectionRepository
                .CountStationsBySyncerIdAsync(syncerIds, ct);

            foreach (var syncer in syncers)
                syncer.Capacity = syncerIdToCapacity[syncer.SyncerId];

            await _repoManager.SyncerRepository.UpdateManyAsync(syncers, session, ct);
        }

        private async Task AddTrafficLightsAsync(
            IEnumerable<ObjectId> candidates,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            foreach (var id in candidates)
            {
                var tl = new TrafficLight { StationId = id };

                tl = await _repoManager.TrafficLightRepository.AddOneAsync(tl, session, ct);

                _logger.LogInformation("New traffic light refers to Station Id: {id}", tl.StationId);
            }
        }

        private async Task DeleteTrafficLightsAsync(
            IEnumerable<ObjectId> candidates,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            foreach (var id in candidates)
                if (await _repoManager.TrafficLightRepository.DeleteByStationIdAsync(id, session, ct))
                    _logger.LogInformation("Traffic light with Station Id: {id} deleted.", id);
                else
                    throw new InvalidOperationException("An error occurred during traffic lights deletion. Operation cancelled.");
        }

        private async Task UpdateTrafficLightsAsync(
            IEnumerable<ObjectId>? oldComStationIds,
            IEnumerable<ObjectId>? newComStationIds,
            IClientSessionHandle? session = null,
            CancellationToken ct = default)
        {
            if (oldComStationIds is not null)
                await DeleteTrafficLightsAsync(oldComStationIds.ToList(), session, ct);
            if (newComStationIds is not null)
                await AddTrafficLightsAsync(newComStationIds.ToList(), session, ct);
        }

        private async Task DeleteSectionsAsync(ObjectId id, IClientSessionHandle? session = null, CancellationToken ct = default)
        {
            if (!await _repoManager.SectionRepository.DeleteByRouteIdAsync(id, session, ct))
                throw new InvalidOperationException("Operation of adding route could not be completed due to sections deletion error.");
        }

        private List<ObjectId> ExtractStationIds(List<Direction> directions) => directions
            .SelectMany(d => new[] { d.From, d.To })
            .Distinct()
            .ToList();
    }
}
