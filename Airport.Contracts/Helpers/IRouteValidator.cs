using Airport.Models.DTOs;

namespace Airport.Contracts.Helpers
{
    public interface IRouteValidator
    {
        Task<HashSet<SectionDTO<ObjectId>>> ValidateRouteAsync(
            List<DirectionDTO> directions,
            Dictionary<ObjectId, int>? comStationIds,
            CancellationToken ct = default);
    }
}
