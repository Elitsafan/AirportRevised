using Airport.Models.DTOs;
using Airport.Presentation.Filters;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(AirportNotStartedFilter))]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightService _flightSvc;

        public FlightsController(IFlightService flightSvc) => _flightSvc = flightSvc;

        // GET: api/Flights
        [HttpGet]
        public async IAsyncEnumerable<FlightDTO> GetAllFlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var flight in _flightSvc.GetAllFlightsAsync(minutesPassed, cancellationToken))
                yield return flight;
        }

        // GET: api/Flights/{id}
        [HttpGet("{id}", Name = "FlightById")]
        public async Task<IActionResult> GetFlightByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var flightDto = await _flightSvc.GetFlightByIdAsync(id, cancellationToken);
            return flightDto is null
                ? NotFound()
                : Ok(flightDto);
        }

        // POST: api/Flights/Landing/...
        [HttpPost("[action]/{id}")]
        public async Task<IActionResult> LandingAsync(
            ObjectId id,
            [FromBody] LandingForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default)
        {
            await _flightSvc.ProcessFlightAsync(id, flightForCreation, cancellationToken);
            return CreatedAtRoute("FlightById", new { id }, flightForCreation);
        }

        // POST: api/Flights/Departure/...
        [HttpPost("[action]/{id}")]
        public async Task<IActionResult> DepartureAsync(
            ObjectId id,
            [FromBody] DepartureForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default)
        {
            await _flightSvc.ProcessFlightAsync(id, flightForCreation, cancellationToken);
            return CreatedAtRoute("FlightById", new { id }, flightForCreation);
        }

        // DELETE: api/Flights/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlightAsync(
            ObjectId id,
            CancellationToken cancellationToken = default) =>
            !await _flightSvc.DeleteFlightAsync(id, cancellationToken)
            ? NotFound()
            : NoContent();
    }
}
