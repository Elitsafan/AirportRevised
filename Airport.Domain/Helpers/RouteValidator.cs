using Airport.Models.DTOs;

namespace Airport.Domain.Helpers
{
    public class RouteValidator : IRouteValidator
    {
        private readonly IRepositoryManager _repoManager;

        public RouteValidator(IRepositoryManager repoManager) => _repoManager = repoManager;

        public async Task<HashSet<SectionDTO<ObjectId>>> ValidateRouteAsync(
            List<DirectionDTO> directions,
            Dictionary<ObjectId, int>? comStationIds,
            CancellationToken ct = default)
        {
            if (comStationIds is null || comStationIds.Count == 0)
                return new();

            var ids = (await ValidateStationsExistenceAsync(directions, ct)).ToList();

            ValidateSectionsStructure(directions, comStationIds, ct);

            var graph = CreateGraph(directions, ids);

            ValidateIfCircularRoute(graph);

            return graph.GetParsedSections(comStationIds.Keys);
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

            var existingStationIds = await _repoManager.StationRepository
                .AreExistAsync(ids, ct);

            var missingIds = ids
                .Except(existingStationIds)
                .ToList();

            if (missingIds.Count != 0)
                throw new MissingRouteStationsException(
                    $"Stations don't exist:\n{string.Join(",\n", missingIds)}.\n" +
                    $"First insert the stations of the route, and then save the route.");

            return ids;
        }

        private void ValidateSectionsStructure(
            List<DirectionDTO> directions,
            Dictionary<ObjectId, int> comStationIds,
            CancellationToken ct = default)
        {
            if (comStationIds.Count == 0)
                return;

            foreach (var direction in directions)
                if (comStationIds.ContainsKey(direction.From) &&
                    comStationIds.ContainsKey(direction.To))
                    throw new InvalidRouteStructureException(
                        $"Route must have least one station that is not a traffic light " +
                        $"between two traffic lights:\n{direction.From}\n{direction.To}");
        }

        public IGraph<ObjectId> CreateGraph(List<DirectionDTO> directions, IEnumerable<ObjectId> ids)
        {
            var graph = new Graph<ObjectId>(ids);

            foreach (var direction in directions)
                graph.AddEdge(direction.From, direction.To);

            return graph;
        }

        private void ValidateIfCircularRoute(IGraph<ObjectId> graph)
        {
            if (graph.IsCircular())
                throw new InvalidRouteStructureException("A circular route is forbidden.");
        }
    }
}
