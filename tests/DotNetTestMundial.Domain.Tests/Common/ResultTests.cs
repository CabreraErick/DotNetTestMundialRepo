// Responsabilidad del archivo: Verifica el contrato de éxito, fallo y acceso a valores de Result.
// Relación en el sistema: Protege el patrón de errores compartido por Domain, Application y API.
using DotNetTestMundial.Domain.Common;

namespace DotNetTestMundial.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericSuccess_PreservesPayloadIncludingDefaultValue()
    {
        var result = Result<int>.Success(0);
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void Failure_PreservesErrorForBothResultTypes(ErrorType type)
    {
        var error = new Error("Test.Error", "Expected error", type);
        Result[] results = { Result.Failure(error), Result<int>.Failure(error) };
        foreach (var result in results)
        {
            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Same(error, result.Error);
            Assert.Equal(type, result.Error!.Type);
        }
    }

    [Fact]
    public void Failure_DoesNotExposeDefaultValueAsSuccessfulPayload()
    {
        var result = Result<int>.Failure(new Error("Test.Error", "Expected error"));
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Factories_RejectInvalidProgrammingUsage()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentException>(() => new Error(" ", "Message"));
        Assert.Throws<ArgumentException>(() => new Error("Code", " "));
    }
}
