using Airport.Contracts.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Airport.Presentation.Filters
{
    public class AirportNotStartedFilter : IAsyncActionFilter
    {
        private readonly IAirportStateProvider _airportStateProvider;

        public AirportNotStartedFilter(IAirportStateProvider airportStateProvider) =>
            _airportStateProvider = airportStateProvider;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_airportStateProvider.HasStarted)
                await next();
            else
                context.Result = new BadRequestObjectResult("Airport needs to start/restart.");
        }
    }
}
