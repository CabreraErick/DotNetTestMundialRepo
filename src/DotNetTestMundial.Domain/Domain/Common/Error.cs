namespace DotNetTestMundial.Domain.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict
}

public sealed record Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    public Error(string code, string message, ErrorType type = ErrorType.Validation)
    {
        // These guards detect invalid error definitions, not business failures.
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
        Type = type;
    }
}
