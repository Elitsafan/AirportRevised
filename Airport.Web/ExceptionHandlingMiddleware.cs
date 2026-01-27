using Airport.Domain.Exceptions;
using MongoDB.Driver;
using System.Text.Json;

namespace Airport.Web
{
    internal sealed class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) => _logger = logger;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                await HandleExceptionAsync(context, e);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
        {
            httpContext.Response.ContentType = "application/json";

            httpContext.Response.StatusCode = exception switch
            {
                MongoClientException => StatusCodes.Status500InternalServerError,
                MongoWriteException => StatusCodes.Status500InternalServerError,
                MongoConnectionException => StatusCodes.Status500InternalServerError,
                InvalidRouteStructureException => StatusCodes.Status400BadRequest,
                MissingRouteStationsException => StatusCodes.Status400BadRequest,
                ArgumentNullException => StatusCodes.Status400BadRequest,
                AirportNotStartedException => StatusCodes.Status400BadRequest,
                EntityNotFoundException => StatusCodes.Status404NotFound,
                LogicProvisionFailedException => StatusCodes.Status404NotFound,
                OperationCanceledException => 444,
                TimeoutException => StatusCodes.Status503ServiceUnavailable,
                // TODO: fix invalidoperationexception
                //InvalidOperationException => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };

            var displayMessage = exception switch
            {
                // Whitelist safe messages
                AirportNotStartedException or
                EntityNotFoundException or
                ArgumentNullException or
                OperationCanceledException or
                InvalidOperationException or
                LogicProvisionFailedException or
                MissingRouteStationsException or
                InvalidRouteStructureException or
                TimeoutException => exception.Message,
                // Internal details
                MongoConnectionException or
                MongoClientException or
                MongoWriteException => "Error while connecting to source.",
                _ => "An internal server error occurred. Please contact support."
            };

            var response = new
            {
                error = displayMessage
            };

            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}