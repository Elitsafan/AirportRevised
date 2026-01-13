using Airport.Models.DTOs;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IStationService
    {
        IAsyncEnumerable<StationDTO> GetAllStationsAsync(CancellationToken cancellationToken = default);
        Task<StationDTO?> GetStationByIdAsync(ObjectId id, CancellationToken cancellationToken = default);
        Task<UpdateResult> UpdateStationAsync(
            ObjectId id, 
            StationForUpdateDTO stationForUpdate, 
            CancellationToken cancellationToken = default);
        Task<ObjectId> SaveStationAsync(
            StationForCreationDTO stationForCreationDTO, 
            CancellationToken cancellationToken = default);
        Task<bool> DeleteStationAsync(ObjectId id, CancellationToken cancellationToken = default);
    }
}
