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
    public class StationsController : ControllerBase
    {
        private readonly IStationService _stationSvc;

        public StationsController(IStationService stationSvc) => _stationSvc = stationSvc;

        // GET: api/Stations
        [HttpGet]
        public async IAsyncEnumerable<StationDTO> GetAllStationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var station in _stationSvc.GetAllStationsAsync(cancellationToken))
                yield return station;
        }

        // GET: api/Stations/{id}
        [HttpGet("{id}", Name = "StationById")]
        public async Task<IActionResult> GetStationByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var stationDto = await _stationSvc.GetStationByIdAsync(id, cancellationToken);
            return stationDto is null
                ? NotFound()
                : Ok(stationDto);
        }

        // POST: api/Stations/{id}
        [HttpPost("[action]/{id}")]
        public async Task<IActionResult> PostStationAsync(
            [FromBody] StationForCreationDTO stationForCreationDTO,
            CancellationToken cancellationToken = default)
        {
            var stationId = await _stationSvc.SaveStationAsync(stationForCreationDTO, cancellationToken);
            return CreatedAtRoute("StationById", new { id = stationId }, stationForCreationDTO);
        }

        // PUT: api/Stations/{id}
        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateStationAsync(
            ObjectId id,
            [FromBody] StationForUpdateDTO stationForUpdate,
            CancellationToken cancellationToken = default) => await _stationSvc.UpdateStationAsync(
                id,
                stationForUpdate,
                cancellationToken) switch
                {
                    Models.Enums.UpdateResult.Failed => NotFound(),
                    Models.Enums.UpdateResult.Matched => BadRequest("Invalid station"),
                    Models.Enums.UpdateResult.Modified => NoContent(),
                    _ => new StatusCodeResult(StatusCodes.Status500InternalServerError),
                };

        // DELETE: api/Stations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStationAsync(
            ObjectId id,
            CancellationToken cancellationToken = default) =>
            !await _stationSvc.DeleteStationAsync(id, cancellationToken)
            ? NotFound()
            : NoContent();
    }
}
