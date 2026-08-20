using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Application.Services;

public sealed class AuthService(IUserAccountRepository users, IRoleRepository roles, IPasswordHasher passwords, ITokenService tokens)
{
    public AuthenticationResult Login(LoginRequest request)
    {
        var user = users.GetByEmail(request.Email) ?? throw new DomainException("Invalid email or password.");
        if (!user.IsActive || !passwords.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");
        return Build(user, tokens.Issue(user));
    }

    public AuthenticationResult Refresh(string refreshToken)
    {
        var userId = tokens.ValidateRefreshToken(refreshToken) ?? throw new DomainException("The session has expired.");
        var user = users.GetById(userId) ?? throw new DomainException("User not found.");
        if (!user.IsActive) throw new DomainException("The user is inactive.");
        tokens.RevokeRefreshToken(refreshToken);
        return Build(user, tokens.Issue(user));
    }

    public void Logout(string? refreshToken, string? accessToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken)) tokens.RevokeRefreshToken(refreshToken);
        if (!string.IsNullOrWhiteSpace(accessToken)) tokens.RevokeAccessToken(accessToken);
    }

    public AuthUserDto Me(Guid userId)
    {
        var user = users.GetById(userId) ?? throw new DomainException("User not found.");
        return Describe(user);
    }

    private AuthenticationResult Build(Domain.Users.UserAccount user, IssuedTokens issued) =>
        new(issued.AccessToken, issued.RefreshToken, issued.AccessExpiresAtUtc, issued.RefreshExpiresAtUtc,
            Describe(user));

    private AuthUserDto Describe(Domain.Users.UserAccount user)
    {
        var role = roles.GetByCode(user.Role) ?? throw new DomainException("The assigned role no longer exists.");
        return new AuthUserDto(user.Id, user.Name, user.Email, role.Code, role.Name, role.Permissions);
    }
}
