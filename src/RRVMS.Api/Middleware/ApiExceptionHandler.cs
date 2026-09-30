using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RRVMS.Application.Common;
using RRVMS.Domain.Common;

namespace RRVMS.Api.Middleware;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
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
            logger.LogError(exception, "Unhandled request failure for {TraceIdentifier}.", context.TraceIdentifier);
        }
        else
        {
            logger.LogInformation(exception, "Request rejected with status {StatusCode} for {TraceIdentifier}.", status, context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Type = $"https://httpstatuses.io/{status}",
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }
}
