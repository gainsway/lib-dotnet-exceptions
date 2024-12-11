using Gainsway.ExceptionsExtensions.Exceptions;
using Gainsway.ExceptionsExtensions.Filters;
using Gainsway.LiquidApi.Auth.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Unit.Tests.Api.Filters;

[TestFixture]
public class HttpResponseExceptionFilterTest
{
    private IHostEnvironment? _mockEnvironment;
    private HttpResponseExceptionFilter? _filter;

    [SetUp]
    public void SetUp()
    {
        _mockEnvironment = Substitute.For<IHostEnvironment>();
        _filter = new HttpResponseExceptionFilter(
            _mockEnvironment,
            new LoggerFactory().CreateLogger<HttpResponseExceptionFilter>()
        );
    }

    [Test]
    public void OnActionExecuted_NoException_DoesNothing()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        );

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.Null);
        Assert.That(context.ExceptionHandled, Is.False);
    }

    [Test]
    public void OnActionExecuted_BadRequestException_SetsBadRequestResult()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new BadRequestException("Invalid request"),
        };

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<BadRequestResult>());
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnActionExecuted_NotFoundException_SetsNotFoundResult()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new NotFoundException("Resource not found"),
        };

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<NotFoundResult>());
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnActionExecuted_ForbiddenException_SetsForbidResult()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new ForbiddenException("Access denied"),
        };

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnActionExecuted_TaskCanceledException_SetsGatewayTimeoutResult()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new TaskCanceledException(),
        };

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
        var result = (ObjectResult)context.Result;
        Assert.That(StatusCodes.Status504GatewayTimeout, Is.EqualTo(result.StatusCode));
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnActionExecuted_GenericException_SetsInternalServerErrorResult()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new Exception("Something went wrong"),
        };

        _mockEnvironment.EnvironmentName.Returns(Environments.Production);

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
        var result = (ObjectResult)context.Result;
        Assert.That(StatusCodes.Status500InternalServerError, Is.EqualTo(result.StatusCode));
        Assert.That(context.ExceptionHandled, Is.True);
    }

    [Test]
    public void OnActionExecuted_GenericException_IncludesStackTraceInNonProduction()
    {
        var context = new ActionExecutedContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor =
                    new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor(),
            },
            [],
            null
        )
        {
            Exception = new Exception("Something went wrong"),
        };

        _mockEnvironment.EnvironmentName.Returns(Environments.Development);

        _filter.OnActionExecuted(context);

        Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
        var result = (ObjectResult)context.Result;
        Assert.That(result.Value, Is.InstanceOf<ProblemDetails>());
        var problemDetails = (ProblemDetails)result.Value;
        Assert.That(context.Exception.StackTrace, Is.EqualTo(problemDetails.Detail));
        Assert.That(context.ExceptionHandled, Is.True);
    }
}
