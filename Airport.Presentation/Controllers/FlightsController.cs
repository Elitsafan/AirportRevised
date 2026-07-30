using Airport.Models.DTOs;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightService _flightSvc;

        public FlightsController(IFlightService flightSvc) => _flightSvc = flightSvc;

        // GET: api/Flights
        [HttpGet]
        public async IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var flight in _flightSvc.GetAllFlightsAsync(minutesPassed, ct))
                yield return flight;
        }

        // GET: api/Flights/{id}
        [HttpGet("{id}", Name = "FlightById")]
        public async Task<IActionResult> GetFlightByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            var flightDto = await _flightSvc.GetFlightByIdAsync(id, ct);
            return flightDto is null
                ? NotFound()
                : Ok(flightDto);
        }

        // POST: api/Flights/AddLanding
        [HttpPost("[action]")]
        public async Task<IActionResult> AddLandingAsync(
            [FromBody] LandingForCreationDTO flightToCreate,
            CancellationToken ct = default)
        {
            var flightDto = await _flightSvc.AddFlightAsync(flightToCreate, ct);
            return AcceptedAtRoute("FlightById", new { id = flightDto.FlightId }, flightDto);
        }

        // POST: api/Flights/AddDeparture
        [HttpPost("[action]")]
        public async Task<IActionResult> AddDepartureAsync(
            [FromBody] DepartureForCreationDTO flightToCreate,
            CancellationToken ct = default)
        {
            var flightDto = await _flightSvc.AddFlightAsync(flightToCreate, ct);
            return AcceptedAtRoute("FlightById", new { id = flightDto.FlightId }, flightDto);
        }

        // DELETE: api/Flights/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlightAsync(ObjectId id, CancellationToken ct = default) =>
            !await _flightSvc.DeleteFlightAsync(id, ct)
            ? NotFound()
            : NoContent();
    }
}
