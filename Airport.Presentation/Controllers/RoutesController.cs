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
    public class RoutesController : ControllerBase
    {
        private readonly IRouteService _routeSvc;

        public RoutesController(IRouteService routeSvc) => _routeSvc = routeSvc;

        // GET: api/Routes
        [HttpGet]
        public async IAsyncEnumerable<RouteDTO> GetAllRoutesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var route in _routeSvc.GetAllRoutesAsync(cancellationToken))
                yield return route;
        }

        // GET: api/Routes/{id}
        [HttpGet("{id}", Name = "RouteById")]
        public async Task<IActionResult> GetRouteByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default)
        {
            var routeDto = await _routeSvc.GetRouteByIdAsync(id, cancellationToken);
            return routeDto is null
                ? NotFound()
                : Ok(routeDto);
        }

        // POST: api/Routes/{id}
        [HttpPost("[action]/{id}")]
        public async Task<IActionResult> PostRouteAsync(
            [FromBody] RouteForCreationDTO routeForCreationDTO,
            CancellationToken cancellationToken = default)
        {
            var routeId = await _routeSvc.SaveRouteAsync(routeForCreationDTO, cancellationToken);
            return CreatedAtRoute("RouteById", new { id = routeId }, routeForCreationDTO);
        }

        // PUT: api/Routes/{id}
        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateRouteAsync(
            ObjectId id,
            [FromBody] RouteForUpdateDTO routeForUpdate,
            CancellationToken cancellationToken = default) => await _routeSvc.UpdateRouteAsync(
                id,
                routeForUpdate,
                cancellationToken) switch
            {
                Models.Enums.UpdateResult.Failed => NotFound(),
                Models.Enums.UpdateResult.Matched => BadRequest("Invalid route"),
                Models.Enums.UpdateResult.Modified => NoContent(),
                _ => new StatusCodeResult(StatusCodes.Status500InternalServerError),
            };

        // DELETE: api/Routes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteAsync(
            ObjectId id,
            CancellationToken cancellationToken = default) =>
            !await _routeSvc.DeleteRouteAsync(id, cancellationToken)
            ? NotFound()
            : NoContent();
    }
}
