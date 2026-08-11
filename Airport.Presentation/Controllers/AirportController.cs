using Airport.Models;
using Airport.Presentation.Filters;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AirportController : ControllerBase
    {
        private readonly IAirportService _airportService;

        public AirportController(IAirportService airportservice) => _airportService = airportservice;

        // GET: api/Airport/Start
        [HttpGet]
        public async Task<IActionResult> StartAsync(CancellationToken ct = default) =>
            Ok(await _airportService.StartAsync(ct));

        // GET: api/Airport/Restart
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RestartAsync(CancellationToken ct = default) =>
            Ok(await _airportService.RestartAsync(ct));

        // GET: api/Airport/Status
        [HttpGet]
        public async Task<IActionResult> StatusAsync(CancellationToken ct = default) =>
            Ok(await _airportService.GetStatusAsync(ct));

        // GET: api/Airport/Summary
        [HttpGet]
        public async Task<IActionResult> SummaryAsync(
            [FromQuery] GetSummaryParameters parameters,
            CancellationToken ct = default)
        {
            var result = await _airportService.GetSummaryWithMetadataAsync(parameters, ct);
            Response.AddPaginationMetadata(result);
            return Ok(result.Summary);
        }
    }
}
