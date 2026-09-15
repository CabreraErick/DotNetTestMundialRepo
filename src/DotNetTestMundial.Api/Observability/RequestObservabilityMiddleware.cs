// Responsabilidad del archivo: Asigna una identidad de correlación y mide cada solicitud HTTP.
// Relación en el sistema: Envuelve los controladores y agrega campos estructurados a todos sus registros.
using System.Diagnostics;

namespace DotNetTestMundial.Api.Observability;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public const string CorrelationHeaderName = "X-Correlation-ID";
    public const string TraceHeaderName = "X-Trace-ID";
    private const int MaximumCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeaderName] = correlationId;
        // ASP.NET owns the incoming W3C activity. Standalone tests/background hosts
        // receive a fallback activity instead of conflating TraceId with CorrelationId.
        using var fallbackActivity = Activity.Current is null
            ? new Activity("HTTP request").SetIdFormat(ActivityIdFormat.W3C).Start()
            : null;
        var traceId = Activity.Current!.TraceId.ToString();
        context.Response.Headers[TraceHeaderName] = traceId;
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId
        });
        logger.LogDebug("HTTP {Method} {Path} started", context.Request.Method, context.Request.Path);
        var startedAt = Stopwatch.GetTimestamp();
        var statusCode = StatusCodes.Status500InternalServerError;

        try
        {
            await next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled error processing HTTP {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            throw;
        }
        finally
        {
            var level = statusCode >= 500 ? LogLevel.Error
                : statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            logger.Log(level,
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var suppliedValue = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();

        return string.IsNullOrWhiteSpace(suppliedValue) ||
               suppliedValue.Length > MaximumCorrelationIdLength
            ? Guid.NewGuid().ToString("D")
            : suppliedValue;
    }
}
