namespace ReviasMiUs.Application.Dtos;

public sealed record LoginRequest(string Email, string Password);
public sealed record AuthUserDto(Guid Id, string Name, string Email, string Role, string RoleName, IReadOnlyCollection<string> Permissions);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User);
public sealed record AuthenticationResult(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc, DateTime RefreshExpiresAtUtc, AuthUserDto User);
