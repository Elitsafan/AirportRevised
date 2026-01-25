using Airport.Models.DTOs;
using Airport.Models.Enums;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IStationService
    {
        IAsyncEnumerable<StationDTO> GetAllStationsAsync(CancellationToken ct = default);
        Task<StationDTO?> GetStationByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<UpdateResult> UpdateStationAsync(
            ObjectId id, 
            StationForUpdateDTO stationForUpdate, 
            CancellationToken ct = default);
        Task<StationDTO> AddStationAsync(StationForCreationDTO stationForCreationDTO, CancellationToken ct = default);
        Task<bool> DeleteStationAsync(ObjectId id, CancellationToken ct = default);
    }
}
