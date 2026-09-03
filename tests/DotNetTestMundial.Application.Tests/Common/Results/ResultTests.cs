using DotNetTestMundial.Application.Common.Results;

namespace DotNetTestMundial.Application.Tests.Common.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Failure(
            "General.Failure",
            "An unexpected failure occurred.");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result =
            Result<int>.Success(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void GenericFailure_ShouldContainError()
    {
        var error = Error.NotFound(
            "Teams.NotFound",
            "The requested team was not found.");

        var result =
            Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void AccessingValueFromFailedResult_ShouldThrow()
    {
        var error = Error.NotFound(
            "Teams.NotFound",
            "The requested team was not found.");

        var result =
            Result<string>.Failure(error);

        Assert.Throws<InvalidOperationException>(
            () => result.Value);
    }

    [Fact]
    public void SuccessfulResult_WithError_ShouldThrow()
    {
        var error = Error.Failure(
            "General.Failure",
            "Failure.");

        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(
                true,
                error));
    }

    [Fact]
    public void FailedResult_WithoutError_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(
                false,
                Error.None));
    }

    [Fact]
    public void ValidationError_ShouldHaveValidationType()
    {
        var error = Error.Validation(
            "Teams.NameRequired",
            "Team name is required.");

        Assert.Equal(
            ErrorType.Validation,
            error.Type);
    }

    [Fact]
    public void NotFoundError_ShouldHaveNotFoundType()
    {
        var error = Error.NotFound(
            "Teams.NotFound",
            "Team not found.");

        Assert.Equal(
            ErrorType.NotFound,
            error.Type);
    }

    [Fact]
    public void ConflictError_ShouldHaveConflictType()
    {
        var error = Error.Conflict(
            "Teams.AlreadyExists",
            "Team already exists.");

        Assert.Equal(
            ErrorType.Conflict,
            error.Type);
    }

    private sealed class TestResult : Result
    {
        public TestResult(
            bool isSuccess,
            Error error)
            : base(
                isSuccess,
                error)
        {
        }
    }
}