using Airport.Models;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Airport.Presentation.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AirportController : ControllerBase
    {
        private readonly IAirportService _airportService;

        public AirportController(IAirportService airportservice) => _airportService = airportservice;

        // GET: api/Airport/Start
        [HttpGet]
        public async Task<IActionResult> StartAsync(CancellationToken ct) =>
            Ok(await _airportService.StartAsync(ct));

        // GET: api/Airport/Status
        [HttpGet]
        public async Task<IActionResult> StatusAsync(CancellationToken ct) =>
            Ok(await _airportService.GetStatusAsync(ct));

        // GET: api/Airport/Summary
        [HttpGet]
        public async Task<IActionResult> SummaryAsync(
            [FromQuery] GetSummaryParameters parameters,
            CancellationToken ct)
        {
            var summary = await _airportService.GetPagedSummaryAsync(parameters, ct);
            (int LandingsCount, int DeparturesCount) = await _airportService.GetFlightsCountAsync(
                summary.CurrentPage * summary.PageSize,
                ct);
            var metadata = new
            {
                summary.TotalCount,
                summary.PageSize,
                summary.CurrentPage,
                summary.TotalPages,
                summary.HasNext,
                summary.HasPrevious,
                LandingsCount,
                DeparturesCount
            };
            var paginationHeader = "X-Pagination";
            Response.Headers.Add("Access-Control-Expose-Headers", paginationHeader);
            Response.Headers.Add(
                paginationHeader,
                JsonConvert.SerializeObject(
                    metadata,
                    new JsonSerializerSettings()
                    {
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new CamelCaseNamingStrategy()
                        },
                    }));
            return Ok(summary);
        }
    }
}
