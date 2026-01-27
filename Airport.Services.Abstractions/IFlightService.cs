using Airport.Models.DTOs;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IFlightService : IAsyncDisposable
    {
        Task AddFlightAsync(
            ObjectId id,
            FlightForCreationDTO flightForCreation,
            CancellationToken ct = default);
        IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            CancellationToken ct = default);
    }
}