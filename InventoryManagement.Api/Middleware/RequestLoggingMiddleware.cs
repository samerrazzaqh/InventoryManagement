using System.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InventoryManagement.Api.Middleware;

/// <summary>Correlation id (accepted from the caller or generated) + one structured log line per request.</summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public const string CorrelationHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Response.Headers[CorrelationHeader] = correlationId;

        using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Elapsed} ms (user {UserId})",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds, context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "anonymous");
        }
    }

    // Caller-supplied ids are validated to prevent log forging / header injection.
    private static string ResolveCorrelationId(HttpContext context)
    {
        var supplied = context.Request.Headers[CorrelationHeader].FirstOrDefault();
        return supplied is { Length: > 0 and <= 64 } && supplied.All(c => char.IsLetterOrDigit(c) || c == '-')
            ? supplied
            : Guid.NewGuid().ToString("N");
    }
}
