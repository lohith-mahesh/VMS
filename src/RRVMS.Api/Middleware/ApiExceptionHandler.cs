using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RRVMS.Application.Common;
using RRVMS.Domain.Common;

namespace RRVMS.Api.Middleware;

public sealed partial class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    [LoggerMessage(3001, LogLevel.Error, "Unhandled request failure for {TraceIdentifier}.")]
    private static partial void LogUnhandledFailure(ILogger logger, string traceIdentifier, Exception exception);

    [LoggerMessage(3002, LogLevel.Information, "Request rejected with status {StatusCode} for {TraceIdentifier}.")]
    private static partial void LogRejectedRequest(ILogger logger, int statusCode, string traceIdentifier, Exception exception);

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledFailure(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            LogRejectedRequest(logger, status, httpContext.TraceIdentifier, exception);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Type = $"https://httpstatuses.io/{status}",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }
}
