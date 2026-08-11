using Airport.Models.DTOs;
using Airport.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Runtime.CompilerServices;

namespace Airport.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeSvc;

        public RoutesController(IRouteService routeSvc) => _routeSvc = routeSvc;

        // GET: api/Routes
        [HttpGet]
        [AllowAnonymous]
        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var route in _routeSvc.GetAllRoutesAsync(ct))
                yield return route;
        }

        // GET: api/Routes/{id}
        [AllowAnonymous]
        [HttpGet("{id}", Name = "RouteById")]
        public async Task<IActionResult> GetRouteByIdAsync(ObjectId id, CancellationToken ct = default)
        {
            var routeDto = await _routeSvc.GetRouteByIdAsync(id, ct);
            return routeDto is null
                ? NotFound()
                : Ok(routeDto);
        }

        // POST: api/Routes
        [HttpPost("[action]")]
        public async Task<IActionResult> AddRouteAsync(
            [FromBody] RouteForCreationDTO routeToCreate,
            CancellationToken ct = default)
        {
            var routeDto = await _routeSvc.AddRouteAsync(routeToCreate, ct);
            return CreatedAtRoute("RouteById", new { id = routeDto.RouteId }, routeDto);
        }

        // PUT: api/Routes/{id}
        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateRouteAsync(
            ObjectId id,
            [FromBody] RouteForUpdateDTO routeToUpdate,
            CancellationToken ct = default) => await _routeSvc.UpdateRouteAsync(
                id,
                routeToUpdate,
                ct) switch
            {
                Models.Enums.UpdateResult.Failed => NotFound(),
                Models.Enums.UpdateResult.Matched => Ok("Route not changed."),
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
