using Airport.Models.DTOs;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IRouteService
    {
        IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(CancellationToken ct = default);
        Task<RouteDTO?> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<RouteDTO> AddRouteAsync(RouteForCreationDTO routeForCreationDTO, CancellationToken ct = default);
        Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken ct = default);
        Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeForUpdate,
            CancellationToken ct = default);
    }
}
