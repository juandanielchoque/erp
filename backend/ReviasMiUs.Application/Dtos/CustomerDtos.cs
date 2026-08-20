namespace ReviasMiUs.Application.Dtos;

public sealed record CustomerDto(Guid Id, string Name, string Email, bool IsActive, DateTime CreatedAtUtc);
public sealed record CreateCustomerRequest(string Name, string Email);
public sealed record UpdateCustomerRequest(string Name, string Email, bool IsActive);
