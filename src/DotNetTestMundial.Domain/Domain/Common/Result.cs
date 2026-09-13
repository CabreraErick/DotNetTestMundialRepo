// Responsabilidad del archivo: Representa éxito o fallo esperado sin usar excepciones de negocio.
// Relación en el sistema: Entidades y handlers lo usan; API inspecciona su categoría para elegir la respuesta HTTP.
namespace DotNetTestMundial.Domain.Common;

public class Result
{
    public bool IsSuccess => Error is null;
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    private protected Result(Error? error) => Error = error;

    public static Result Success() => new(null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(error);
    }
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    // Accessing Value on a failure is a programming error. Callers must check IsSuccess.
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    private Result(T value) : base(null)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    private Result(Error error) : base(error) { }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(error);
    }
}
