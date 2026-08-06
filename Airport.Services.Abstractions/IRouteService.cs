using Airport.Models.DTOs;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IRouteService : IDisposable
    {
        IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(CancellationToken ct = default);
        Task<RouteDTO> GetRouteByIdAsync(ObjectId routeId, CancellationToken ct = default);
        Task<RouteDTO> AddRouteAsync(RouteForCreationDTO routeToCreate, CancellationToken ct = default);
        Task<UpdateResult> UpdateRouteAsync(
            ObjectId routeId,
            RouteForUpdateDTO routeToUpdate,
            CancellationToken ct = default);
        Task<bool> DeleteRouteAsync(ObjectId routeId, CancellationToken ct = default);
    }
}
