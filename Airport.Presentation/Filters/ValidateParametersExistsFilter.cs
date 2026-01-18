using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Airport.Presentation.Filters
{
    public class ValidateParametersExistsFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var arg in context.ActionArguments.Values)
                if (arg is null)
                {
                    context.Result = new BadRequestObjectResult("Parameters cannot be null.");
                    return;
                }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
