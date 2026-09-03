namespace DotNetTestMundial.Application.Common.Results;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(
        TValue? value,
        bool isSuccess,
        Error error)
        : base(
            isSuccess,
            error)
    {
        _value = value;
    }

    public TValue Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException(
                    "The value of a failed result cannot be accessed.");
            }

            return _value!;
        }
    }

    public static Result<TValue> Success(
        TValue value)
    {
        return new Result<TValue>(
            value,
            true,
            Error.None);
    }

    public new static Result<TValue> Failure(
        Error error)
    {
        return new Result<TValue>(
            default,
            false,
            error);
    }
}