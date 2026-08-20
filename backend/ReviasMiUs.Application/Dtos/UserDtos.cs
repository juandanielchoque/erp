namespace ReviasMiUs.Application.Dtos;

public sealed record UserAccountDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateUserAccountRequest(string Name, string Email, string Role, string Password);
public sealed record UpdateUserRoleRequest(string Role);
public sealed record UpdateUserStatusRequest(bool IsActive);
public sealed record UpdateUserAccountRequest(string Name, string Email, string Role, bool IsActive, string? NewPassword = null);
