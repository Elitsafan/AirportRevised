using Airport.Models.DTOs;
using Airport.Presentation.Filters;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Http;
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
        [HttpGet(Name = "GetAllFlights")]
        public async IAsyncEnumerable<FlightDTO> FlightsAsync(
            int? minutesPassed,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var flight in _flightSvc.GetAllFlightsAsync(minutesPassed, cancellationToken))
                yield return flight;
        }

        // POST: api/Flights/Landing/...
        [HttpPost("[action]/{id}", Name = "Landing")]
        public async Task<IActionResult> LandingAsync(
            ObjectId id,
            [FromBody] LandingForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default)
        {
            await _flightSvc.ProcessFlightAsync(id, flightForCreation, cancellationToken);
            return CreatedAtRoute("Status", StatusCodes.Status201Created);
        }

        // POST: api/Flights/Departure/...
        [HttpPost("[action]/{id}", Name = "Departure")]
        public async Task<IActionResult> DepartureAsync(
            ObjectId id,
            [FromBody] DepartureForCreationDTO flightForCreation,
            CancellationToken cancellationToken = default)
        {
            await _flightSvc.ProcessFlightAsync(id, flightForCreation, cancellationToken);
            return CreatedAtRoute("Status", StatusCodes.Status201Created);
        }
    }
}
