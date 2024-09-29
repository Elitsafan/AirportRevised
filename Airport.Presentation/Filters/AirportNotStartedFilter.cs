using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Airport.Services.Abstractions;

namespace Airport.Presentation.Filters
{
    internal class AirportNotStartedFilter : IAsyncActionFilter
    {
        private readonly IAirportService _airportService;

        public AirportNotStartedFilter(IAirportService airportService) => _airportService = airportService;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_airportService.HasStarted)
                await next();
            else
                context.Result = new BadRequestObjectResult("Airport needs to start/restart.");
        }
    }
}
