// Responsabilidad del archivo: Comprueba correlación, respuesta y campos estructurados del middleware.
// Relación en el sistema: Ejecuta RequestObservabilityMiddleware sin servidor ni base de datos.
using DotNetTestMundial.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetTestMundial.Api.Tests.Observability;

public sealed class RequestObservabilityMiddlewareTests
{
    [Fact]
    public async Task Invoke_PreservesClientCorrelationIdAcrossRequestResponseAndLogScope()
    {
        const string suppliedCorrelationId = "swagger-check-001";
        var logger = new RecordingLogger<RequestObservabilityMiddleware>();
        string? traceSeenByEndpoint = null;
        var middleware = new RequestObservabilityMiddleware(
            context =>
            {
                traceSeenByEndpoint = context.TraceIdentifier;
                context.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            logger);
        var context = CreateContext();
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationHeaderName] = suppliedCorrelationId;

        await middleware.InvokeAsync(context);

        Assert.Equal(suppliedCorrelationId, traceSeenByEndpoint);
        Assert.Equal(
            suppliedCorrelationId,
            context.Response.Headers[RequestObservabilityMiddleware.CorrelationHeaderName].ToString());
        Assert.Contains(logger.Scopes, scope =>
            scope.TryGetValue("CorrelationId", out var value) &&
            Equals(value, suppliedCorrelationId));
        var completion = Assert.Single(logger.Entries.Where(entry => entry.Level == LogLevel.Information));
        Assert.Equal(StatusCodes.Status201Created, completion.Properties["StatusCode"]);
        Assert.True(Convert.ToDouble(completion.Properties["ElapsedMilliseconds"]) >= 0);
    }

    [Fact]
    public async Task Invoke_GeneratesGuidWhenHeaderIsMissing()
    {
        var logger = new RecordingLogger<RequestObservabilityMiddleware>();
        var middleware = new RequestObservabilityMiddleware(_ => Task.CompletedTask, logger);
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        Assert.True(Guid.TryParse(context.TraceIdentifier, out _));
        Assert.Equal(
            context.TraceIdentifier,
            context.Response.Headers[RequestObservabilityMiddleware.CorrelationHeaderName].ToString());
    }

    [Fact]
    public async Task Invoke_LogsUnhandledExceptionAndRethrowsIt()
    {
        var logger = new RecordingLogger<RequestObservabilityMiddleware>();
        var expected = new InvalidOperationException("Failure injected by test");
        var middleware = new RequestObservabilityMiddleware(_ => throw expected, logger);
        var context = CreateContext();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));

        Assert.Same(expected, actual);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error && ReferenceEquals(entry.Exception, expected));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Error &&
            Equals(entry.Properties.GetValueOrDefault("StatusCode"), StatusCodes.Status500InternalServerError));
    }

    [Theory]
    [InlineData(200, LogLevel.Information)]
    [InlineData(400, LogLevel.Warning)]
    [InlineData(409, LogLevel.Warning)]
    [InlineData(500, LogLevel.Error)]
    public async Task Invoke_UsesStatusAppropriateLevelAndIndependentTraceId(int status, LogLevel level)
    {
        var logger = new RecordingLogger<RequestObservabilityMiddleware>();
        var middleware = new RequestObservabilityMiddleware(context =>
        {
            context.Response.StatusCode = status;
            return Task.CompletedTask;
        }, logger);
        var context = CreateContext();
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationHeaderName] = "business-operation";
        await middleware.InvokeAsync(context);
        var traceId = context.Response.Headers[RequestObservabilityMiddleware.TraceHeaderName].ToString();
        Assert.Equal(32, traceId.Length);
        Assert.NotEqual("business-operation", traceId);
        Assert.Contains(logger.Scopes, scope => Equals(scope["TraceId"], traceId));
        Assert.Contains(logger.Entries, entry => entry.Level == level &&
            Equals(entry.Properties.GetValueOrDefault("StatusCode"), status));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/teams";
        return context;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = new();
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(ToDictionary(state));
            return EmptyScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, exception, ToDictionary(state)));
        }

        private static IReadOnlyDictionary<string, object?> ToDictionary<TState>(TState state) =>
            state is IEnumerable<KeyValuePair<string, object?>> properties
                ? properties.ToDictionary(property => property.Key, property => property.Value)
                : new Dictionary<string, object?>();

        private sealed class EmptyScope : IDisposable
        {
            public static readonly EmptyScope Instance = new();
            public void Dispose()
            {
            }
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties);
}
