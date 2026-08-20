namespace ReviasMiUs.Application.Dtos;

public sealed record RoleDto(Guid Id, string Code, string Name, string Description, bool IsSystem, int UserCount, IReadOnlyCollection<string> Permissions);
public sealed record CreateRoleRequest(string Code, string Name, string Description, IReadOnlyCollection<string> Permissions);
public sealed record UpdateRoleRequest(string Name, string Description, IReadOnlyCollection<string> Permissions);
public sealed record PermissionDto(string Code, string Name, string Description, string Group, bool AdministratorOnly);
