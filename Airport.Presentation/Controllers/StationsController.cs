using Airport.Models.DTOs;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly IStationService _stationSvc;

        public StationsController(IStationService stationSvc) => _stationSvc = stationSvc;

        // GET: api/Stations
        [HttpGet]
        public async IAsyncEnumerable<StationDTO> GetAllStationsAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var station in _stationSvc.GetAllStationsAsync(ct))
                yield return station;
        }

        // GET: api/Stations/{id}
        [HttpGet("{id}", Name = "StationById")]
        public async Task<IActionResult> GetStationByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            var stationDto = await _stationSvc.GetStationByIdAsync(id, ct);
            return stationDto is null
                ? NotFound()
                : Ok(stationDto);
        }

        // POST: api/Stations
        [HttpPost("[action]")]
        public async Task<IActionResult> AddStationAsync(
            [FromBody] StationForCreationDTO stationForCreationDTO,
            CancellationToken ct = default)
        {
            var stationDto = await _stationSvc.AddStationAsync(stationForCreationDTO, ct);
            return CreatedAtRoute("StationById", new { id = stationDto.StationId }, stationDto);
        }

        // PUT: api/Stations/{id}
        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateStationAsync(
            ObjectId id,
            [FromBody] StationForUpdateDTO stationForUpdate,
            CancellationToken ct = default) => await _stationSvc.UpdateStationAsync(
                id,
                stationForUpdate,
                ct) switch
            {
                Models.Enums.UpdateResult.Failed => NotFound(),
                Models.Enums.UpdateResult.Matched => BadRequest("Invalid station"),
                Models.Enums.UpdateResult.Modified => NoContent(),
                _ => new StatusCodeResult(StatusCodes.Status500InternalServerError),
            };

        // DELETE: api/Stations/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStationAsync(ObjectId id, CancellationToken ct = default) =>
            !await _stationSvc.DeleteStationAsync(id, ct)
            ? NotFound()
            : NoContent();
    }
}