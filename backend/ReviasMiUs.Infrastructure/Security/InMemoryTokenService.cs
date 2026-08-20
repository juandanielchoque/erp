using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Users;

namespace ReviasMiUs.Infrastructure.Security;

public sealed class InMemoryTokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, AccessSession> _accessTokens = new();
    private readonly ConcurrentDictionary<string, RefreshSession> _refreshTokens = new();

    public IssuedTokens Issue(UserAccount user)
    {
        CleanupExpired();
        var accessToken = CreateToken();
        var refreshToken = CreateToken();
        var accessExpiry = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);
        _accessTokens[HashToken(accessToken)] = new AccessSession(new AuthenticatedIdentity(user.Id, user.Name, user.Email, user.Role), accessExpiry);
        _refreshTokens[HashToken(refreshToken)] = new RefreshSession(user.Id, refreshExpiry);
        return new IssuedTokens(accessToken, refreshToken, accessExpiry, refreshExpiry);
    }

    public AuthenticatedIdentity? ValidateAccessToken(string token)
    {
        if (!_accessTokens.TryGetValue(HashToken(token), out var session)) return null;
        if (session.ExpiresAtUtc > DateTime.UtcNow) return session.Identity;
        _accessTokens.TryRemove(HashToken(token), out _);
        return null;
    }

    public Guid? ValidateRefreshToken(string token)
    {
        if (!_refreshTokens.TryGetValue(HashToken(token), out var session)) return null;
        return session.ExpiresAtUtc > DateTime.UtcNow ? session.UserId : null;
    }

    public void RevokeAccessToken(string token) => _accessTokens.TryRemove(HashToken(token), out _);
    public void RevokeRefreshToken(string token) => _refreshTokens.TryRemove(HashToken(token), out _);

    private void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var item in _accessTokens.Where(item => item.Value.ExpiresAtUtc <= now)) _accessTokens.TryRemove(item.Key, out _);
        foreach (var item in _refreshTokens.Where(item => item.Value.ExpiresAtUtc <= now)) _refreshTokens.TryRemove(item.Key, out _);
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private sealed record AccessSession(AuthenticatedIdentity Identity, DateTime ExpiresAtUtc);
    private sealed record RefreshSession(Guid UserId, DateTime ExpiresAtUtc);
}
