using Airport.Models.DTOs;
using MongoDB.Bson;

namespace Airport.Services.Abstractions
{
    public interface IFlightService : IAsyncDisposable
    {
        Task ProcessFlightAsync(
            ObjectId id,
            FlightForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default);
        IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            CancellationToken cancellationToken = default);
    }
}