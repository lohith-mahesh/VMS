namespace RRVMS.Api.Middleware;

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        context.TraceIdentifier = IsValid(candidate) ? candidate! : Guid.NewGuid().ToString("N");
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = context.TraceIdentifier;
            return Task.CompletedTask;
        });
        await next(context);
    }

    private static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 100 && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
}
