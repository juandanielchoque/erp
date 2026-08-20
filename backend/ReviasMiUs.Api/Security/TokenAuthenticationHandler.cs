using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ReviasMiUs.Application.Abstractions;

namespace ReviasMiUs.Api.Security;

public sealed class TokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITokenService tokens,
    IUserAccountRepository users,
    IRoleRepository roles) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());
        var token = authorization[7..].Trim();
        var identity = tokens.ValidateAccessToken(token);
        if (identity is null) return Task.FromResult(AuthenticateResult.Fail("Invalid or expired access token."));

        var user = users.GetById(identity.UserId);
        var role = user is null ? null : roles.GetByCode(user.Role);
        if (user is null || !user.IsActive || role is null)
            return Task.FromResult(AuthenticateResult.Fail("The user or assigned role is unavailable."));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role.Code)
        };
        claims.AddRange(role.Permissions.Select(permission => new Claim("permission", permission)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
