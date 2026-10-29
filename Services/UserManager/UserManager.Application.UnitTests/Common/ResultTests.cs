using UserManager.Application.Common;

namespace UserManager.Application.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Failure_IsValidationError()
    {
        // Act
        var result = Result<Guid>.Failure("Name", "O nome é obrigatório.");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public void NotFound_CarriesNotFoundTypeAndError()
    {
        // Act
        var result = Result<Guid>.NotFound("UserId", "Usuário não encontrado.");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UserId", error.Property);
        Assert.Equal("Usuário não encontrado.", error.Message);
    }

    [Fact]
    public void Conflict_CarriesConflictTypeAndError()
    {
        // Act
        var result = Result<Guid>.Conflict("Email", "E-mail já existe.");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Email", Assert.Single(result.Errors).Property);
    }

    [Fact]
    public void Success_HasNoErrorType()
    {
        // Act
        var result = Result<Guid>.Success(Guid.NewGuid());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }
}
