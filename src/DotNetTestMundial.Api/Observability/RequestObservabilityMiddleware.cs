// Responsabilidad del archivo: Asigna una identidad de correlación y mide cada solicitud HTTP.
// Relación en el sistema: Envuelve los controladores y agrega campos estructurados a todos sus registros.
using System.Diagnostics;

namespace DotNetTestMundial.Api.Observability;

public sealed class RequestObservabilityMiddleware(
    RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public const string CorrelationHeaderName = "X-Correlation-ID";
    private const int MaximumCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeaderName] = correlationId;

        using var scope = logger.BeginScope("CorrelationId: {CorrelationId}", correlationId);
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
            logger.LogInformation(
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
