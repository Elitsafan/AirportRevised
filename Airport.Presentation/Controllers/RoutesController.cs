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
    [ServiceFilter(typeof(AirportNotStartedFilter))]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeSvc;

        public RoutesController(IRouteService routeSvc) => _routeSvc = routeSvc;

        // GET: api/Routes
        [HttpGet]
        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var route in _routeSvc.GetAllRoutesAsync(ct))
                yield return route;
        }

        // GET: api/Routes/{id}
        [HttpGet("{id}", Name = "RouteById")]
        public async Task<IActionResult> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default) => 
            await _routeSvc.GetRouteByIdAsync(id, ct) is null
            ? NotFound()
            : Ok(await _routeSvc.GetRouteByIdAsync(id, ct));

        // POST: api/Routes/{id}
        [HttpPost("[action]/{id}")]
        [ServiceFilter(typeof(ValidateParametersExistsFilter))]
        public async Task<IActionResult> PostRouteAsync(
            [FromBody] RouteForCreationDTO routeForCreationDTO,
            CancellationToken ct = default)
        {
            var routeId = await _routeSvc.AddRouteAsync(routeForCreationDTO, ct);
            return CreatedAtRoute("RouteById", new { id = routeId }, routeForCreationDTO);
        }

        // PUT: api/Routes/{id}
        [HttpPut("[action]/{id}")]
        [ServiceFilter(typeof(ValidateParametersExistsFilter))]
        public async Task<IActionResult> UpdateRouteAsync(
            ObjectId id,
            [FromBody] RouteForUpdateDTO routeForUpdate,
            CancellationToken ct = default) => await _routeSvc.UpdateRouteAsync(
                id,
                routeForUpdate,
                ct) switch
            {
                Models.Enums.UpdateResult.Failed => NotFound(),
                Models.Enums.UpdateResult.Matched => BadRequest("Invalid route"),
                Models.Enums.UpdateResult.Modified => NoContent(),
                _ => new StatusCodeResult(StatusCodes.Status500InternalServerError),
            };

        // DELETE: api/Routes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteAsync(ObjectId id, CancellationToken ct = default) =>
            !await _routeSvc.DeleteRouteAsync(id, ct)
            ? NotFound()
            : NoContent();
    }
}
