using Airport.Models.DTOs;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IFlightService : IAsyncDisposable
    {
        IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(int? minutesPassed, CancellationToken ct = default);
        Task<FlightDTO> GetFlightByIdAsync(ObjectId id, CancellationToken ct = default);
        Task<FlightDTO> AddFlightAsync(FlightForCreationDTO flightToCreate, CancellationToken ct = default);
        Task<bool> DeleteFlightAsync(ObjectId id, CancellationToken ct = default);
    }
}