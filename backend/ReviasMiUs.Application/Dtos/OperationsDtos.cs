namespace ReviasMiUs.Application.Dtos;

public sealed record RestaurantTableDto(string Number, string Area, int Seats, string Status, Guid? CurrentOrderId, DateTime? OccupiedAtUtc, string QrToken);
public sealed record CreateRestaurantTableRequest(string Number, string Area, int Seats);
public sealed record UpdateRestaurantTableRequest(string Number, string Area, int Seats);
public sealed record OpenCashShiftRequest(Guid UserId, string Terminal, decimal OpeningCash);
public sealed record CloseCashShiftRequest(decimal CountedCash);
public sealed record CashMovementRequest(string Type, decimal Amount, string Reason);
public sealed record CashShiftDto(Guid Id, Guid UserId, string UserName, string Terminal, string Status, decimal OpeningCash, decimal ExpectedCash, decimal? CountedCash, decimal? Difference, decimal GrossSales, DateTime OpenedAtUtc, DateTime? ClosedAtUtc);
public sealed record KitchenTicketLineDto(string ProductName, int Quantity);
public sealed record KitchenTicketDto(Guid Id, Guid OrderId, string OrderNumber, string? TableNumber, string Status, DateTime CreatedAtUtc, DateTime? StartedAtUtc, DateTime? ReadyAtUtc, IReadOnlyCollection<KitchenTicketLineDto> Lines);
public sealed record FiscalDocumentDto(Guid Id, Guid OrderId, string Type, string Number, string CustomerName, string? CustomerTaxId, decimal TaxableAmount, decimal TaxAmount, decimal TotalAmount, string Status, DateTime IssuedAtUtc);
public sealed record PaymentPartRequest(string Method, decimal Amount);
public sealed record ReceiptPaymentDto(string Method, decimal Amount);

public sealed record CompletePosSaleRequest(
    Guid UserId,
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderLineRequest> Lines,
    string ServiceType,
    string? TableNumber,
    string PaymentMethod,
    string DocumentType,
    string? CustomerTaxId,
    string? Notes,
    IReadOnlyCollection<PaymentPartRequest>? Payments = null);

public sealed record CompletePosSaleDto(
    SalesOrderDto Order,
    CashShiftDto Shift,
    KitchenTicketDto KitchenTicket,
    FiscalDocumentDto FiscalDocument);

public sealed record PublicMenuProductDto(Guid Id, string Name, string Category, decimal UnitPrice, int AvailableQuantity);
public sealed record PublicTableMenuDto(string BusinessName, string TableNumber, string Area, int Seats, string Currency, IReadOnlyCollection<PublicMenuProductDto> Products);
public sealed record CreatePublicTableOrderRequest(string? GuestName, string? Notes, IReadOnlyCollection<CreateOrderLineRequest> Lines);
public sealed record PublicTableOrderDto(Guid OrderId, string OrderNumber, string TableNumber, decimal TotalAmount, string Status, DateTime CreatedAtUtc);
public sealed record PrintableReceiptDto(ReceiptTemplateDto Template, FiscalDocumentDto Document, SalesOrderDto Order, string CashierName, IReadOnlyCollection<ReceiptPaymentDto> Payments);
