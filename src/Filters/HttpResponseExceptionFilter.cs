using Gainsway.ExceptionsExtensions.Exceptions;
using Gainsway.LiquidApi.Auth.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gainsway.ExceptionsExtensions.Filters;

public class HttpResponseExceptionFilter(
    IHostEnvironment environment,
    ILogger<HttpResponseExceptionFilter> logger
) : IActionFilter, IOrderedFilter
{
    public int Order => int.MaxValue - 10;

    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is null)
        {
            return;
        }

        (int statusCode, string title) = MapException(context.Exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
        };
        if (!environment.IsProduction())
        {
            problemDetails.Detail = context.Exception.StackTrace;
        }

        context.Result = GetActionResult(context.Exception, problemDetails);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                context.Exception,
                "An error occurred while processing the request. StatusCode: {statusCode}, Request: {request}",
                statusCode,
                problemDetails.Instance
            );
        }
        context.ExceptionHandled = true;
    }

    private static (int statusCode, string title) MapException(Exception exception) =>
        exception switch
        {
            TaskCanceledException _ => (StatusCodes.Status504GatewayTimeout, "Request Timeout"),
            BadRequestException be => (
                StatusCodes.Status400BadRequest,
                be.Message ?? "Bad Request"
            ),
            NotFoundException ne => (StatusCodes.Status404NotFound, ne.Message ?? "Not Found"),
            ForbiddenException fe => (StatusCodes.Status403Forbidden, fe.Message ?? "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

    private static IActionResult GetActionResult(
        Exception exception,
        ProblemDetails problemDetails
    ) =>
        exception switch
        {
            BadRequestException _ => new BadRequestResult(),
            NotFoundException _ => new NotFoundResult(),
            ForbiddenException _ => new ForbidResult(),
            _ => new ObjectResult(problemDetails) { StatusCode = problemDetails.Status },
        };
}
