using TSTHRMS.Application.Auth.Dtos;

namespace TSTHRMS.UnitTests.Auth;

public class AuthResultTests
{
    [Fact]
    public void Success_carries_the_response_and_refresh_token_and_no_error()
    {
        var response = new LoginResponse(
            "access-token",
            DateTimeOffset.UtcNow,
            new AuthenticatedUserDto(Guid.NewGuid(), "a@b.com", Guid.NewGuid(), []));

        var result = AuthResult.Success(response, "refresh-token");

        Assert.True(result.Succeeded);
        Assert.Same(response, result.Response);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_carries_the_error_and_no_response_or_refresh_token()
    {
        var result = AuthResult.Failure("bad credentials");

        Assert.False(result.Succeeded);
        Assert.Null(result.Response);
        Assert.Null(result.RefreshToken);
        Assert.Equal("bad credentials", result.Error);
    }
}
