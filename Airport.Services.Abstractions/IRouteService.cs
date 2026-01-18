using Airport.Models.DTOs;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IRouteService
    {
        IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(CancellationToken cancellationToken = default);
        Task<RouteDTO?> GetRouteByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<ObjectId> AddRouteAsync(
            RouteForCreationDTO routeForCreationDTO,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteRouteAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<UpdateResult> UpdateRouteAsync(
            ObjectId id,
            RouteForUpdateDTO routeForUpdate,
            CancellationToken cancellationToken = default);
    }
}
