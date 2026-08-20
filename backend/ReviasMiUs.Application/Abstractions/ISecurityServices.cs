using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public sealed record AuthenticatedIdentity(Guid UserId, string Name, string Email, string Role);
public sealed record IssuedTokens(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc, DateTime RefreshExpiresAtUtc);

public interface ITokenService
{
    IssuedTokens Issue(UserAccount user);
    AuthenticatedIdentity? ValidateAccessToken(string token);
    Guid? ValidateRefreshToken(string token);
    void RevokeAccessToken(string token);
    void RevokeRefreshToken(string token);
}
