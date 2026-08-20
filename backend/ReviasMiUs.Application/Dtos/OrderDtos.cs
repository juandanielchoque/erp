namespace ReviasMiUs.Application.Dtos;

public sealed record CreateOrderLineRequest(Guid ProductId, int Quantity);

public sealed record CreateSalesOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineRequest> Lines,
    DateTime? ValidUntilUtc = null,
    string? Notes = null,
    string ServiceType = "Retail",
    string? TableNumber = null);

public sealed record OrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record SalesOrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ValidUntilUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    string? Notes,
    string ServiceType,
    string? TableNumber,
    decimal TotalAmount,
    IReadOnlyCollection<OrderLineDto> Lines);
