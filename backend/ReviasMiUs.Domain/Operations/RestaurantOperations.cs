using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Operations;

public enum RestaurantTableStatus { Free, Reserved, Occupied, Cleaning }
public enum CashShiftStatus { Open, Closed }
public enum PaymentMethod { Cash, Card, Yape, Plin, BankTransfer }
public enum CashMovementType { Payment, CashIn, CashOut }
public enum KitchenTicketStatus { Pending, Preparing, Ready, Delivered, Cancelled }
public enum FiscalDocumentType { Receipt, Invoice }
public enum FiscalDocumentStatus { Issued, Accepted, Rejected, Cancelled }

public sealed class RestaurantTable : Entity
{
    public string Number { get; private set; }
    public string Area { get; private set; }
    public int Seats { get; private set; }
    public RestaurantTableStatus Status { get; private set; }
    public Guid? CurrentOrderId { get; private set; }
    public DateTime? OccupiedAtUtc { get; private set; }
    public string QrToken { get; private set; }

    public RestaurantTable(string number, string area, int seats)
    {
        if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(area) || seats <= 0)
        {
            throw new DomainException("Table number, area and positive seat count are required.");
        }

        Number = number.Trim();
        Area = area.Trim();
        Seats = seats;
        Status = RestaurantTableStatus.Free;
        QrToken = CreateQrToken();
    }

    public void Occupy(Guid orderId)
    {
        if (orderId == Guid.Empty || Status is RestaurantTableStatus.Occupied or RestaurantTableStatus.Cleaning)
        {
            throw new DomainException("The table is not available.");
        }

        Status = RestaurantTableStatus.Occupied;
        CurrentOrderId = orderId;
        OccupiedAtUtc = DateTime.UtcNow;
    }

    public void MarkCleaning()
    {
        if (Status != RestaurantTableStatus.Occupied)
        {
            throw new DomainException("Only occupied tables can be sent to cleaning.");
        }
        Status = RestaurantTableStatus.Cleaning;
    }

    public void Release()
    {
        Status = RestaurantTableStatus.Free;
        CurrentOrderId = null;
        OccupiedAtUtc = null;
    }

    public void UpdateDetails(string number, string area, int seats)
    {
        if (Status != RestaurantTableStatus.Free)
            throw new DomainException("Only free tables can be edited.");
        if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(area) || seats <= 0)
            throw new DomainException("Table number, area and positive seat count are required.");
        Number = number.Trim();
        Area = area.Trim();
        Seats = seats;
    }

    public void RegenerateQrToken() => QrToken = CreateQrToken();

    private static string CreateQrToken() => Guid.NewGuid().ToString("N");
}

public sealed record CashMovement(
    Guid Id,
    Guid? OrderId,
    CashMovementType Type,
    PaymentMethod Method,
    decimal Amount,
    string Reason,
    DateTime CreatedAtUtc);

public sealed class CashShift : Entity
{
    private readonly List<CashMovement> _movements = [];

    public Guid UserId { get; private set; }
    public string Terminal { get; private set; }
    public decimal OpeningCash { get; private set; }
    public CashShiftStatus Status { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public decimal? CountedCash { get; private set; }
    public IReadOnlyCollection<CashMovement> Movements => _movements.AsReadOnly();
    public decimal GrossSales => _movements.Where(item => item.Type == CashMovementType.Payment).Sum(item => item.Amount);
    public decimal ExpectedCash => OpeningCash + _movements.Where(item => item.Method == PaymentMethod.Cash).Sum(item => item.Type switch
    {
        CashMovementType.Payment or CashMovementType.CashIn => item.Amount,
        CashMovementType.CashOut => -item.Amount,
        _ => 0
    });
    public decimal? Difference => CountedCash.HasValue ? CountedCash.Value - ExpectedCash : null;

    public CashShift(Guid userId, string terminal, decimal openingCash)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(terminal) || openingCash < 0)
        {
            throw new DomainException("User, terminal and a valid opening cash amount are required.");
        }

        UserId = userId;
        Terminal = terminal.Trim();
        OpeningCash = openingCash;
        Status = CashShiftStatus.Open;
        OpenedAtUtc = DateTime.UtcNow;
    }

    public void RegisterPayment(Guid orderId, PaymentMethod method, decimal amount)
    {
        EnsureOpen();
        if (orderId == Guid.Empty || amount <= 0)
        {
            throw new DomainException("A valid order and payment amount are required.");
        }
        _movements.Add(new CashMovement(Guid.NewGuid(), orderId, CashMovementType.Payment, method, amount, "Sale payment", DateTime.UtcNow));
    }

    public void AddCashMovement(CashMovementType type, decimal amount, string reason)
    {
        EnsureOpen();
        if (type == CashMovementType.Payment || amount <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A valid cash movement and reason are required.");
        }
        _movements.Add(new CashMovement(Guid.NewGuid(), null, type, PaymentMethod.Cash, amount, reason.Trim(), DateTime.UtcNow));
    }

    public void Close(decimal countedCash)
    {
        EnsureOpen();
        if (countedCash < 0)
        {
            throw new DomainException("Counted cash cannot be negative.");
        }
        CountedCash = countedCash;
        Status = CashShiftStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    private void EnsureOpen()
    {
        if (Status != CashShiftStatus.Open)
        {
            throw new DomainException("The cash shift is closed.");
        }
    }
}

public sealed class KitchenTicket : Entity
{
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; }
    public string? TableNumber { get; private set; }
    public KitchenTicketStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? ReadyAtUtc { get; private set; }

    public KitchenTicket(Guid orderId, string orderNumber, string? tableNumber)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        TableNumber = tableNumber;
        Status = KitchenTicketStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Advance()
    {
        Status = Status switch
        {
            KitchenTicketStatus.Pending => KitchenTicketStatus.Preparing,
            KitchenTicketStatus.Preparing => KitchenTicketStatus.Ready,
            KitchenTicketStatus.Ready => KitchenTicketStatus.Delivered,
            _ => throw new DomainException("The kitchen ticket cannot be advanced.")
        };
        if (Status == KitchenTicketStatus.Preparing) StartedAtUtc = DateTime.UtcNow;
        if (Status == KitchenTicketStatus.Ready) ReadyAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is KitchenTicketStatus.Delivered or KitchenTicketStatus.Cancelled)
            throw new DomainException("The kitchen ticket cannot be cancelled.");
        Status = KitchenTicketStatus.Cancelled;
    }
}

public sealed class FiscalDocument : Entity
{
    public Guid OrderId { get; private set; }
    public FiscalDocumentType Type { get; private set; }
    public string Number { get; private set; }
    public string CustomerName { get; private set; }
    public string? CustomerTaxId { get; private set; }
    public decimal TaxableAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public FiscalDocumentStatus Status { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }

    public FiscalDocument(Guid orderId, FiscalDocumentType type, string number, string customerName, string? customerTaxId, decimal totalAmount, decimal taxRate = 18m)
    {
        if (type == FiscalDocumentType.Invoice && (string.IsNullOrWhiteSpace(customerTaxId) || customerTaxId.Length != 11))
        {
            throw new DomainException("An 11-digit RUC is required for an invoice.");
        }
        OrderId = orderId;
        Type = type;
        Number = number;
        CustomerName = customerName;
        CustomerTaxId = string.IsNullOrWhiteSpace(customerTaxId) ? null : customerTaxId.Trim();
        TotalAmount = totalAmount;
        if (taxRate is < 0 or > 100) throw new DomainException("Tax rate must be between 0 and 100.");
        TaxableAmount = Math.Round(totalAmount / (1 + taxRate / 100m), 2);
        TaxAmount = totalAmount - TaxableAmount;
        Status = FiscalDocumentStatus.Issued;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == FiscalDocumentStatus.Cancelled)
            throw new DomainException("The fiscal document is already cancelled.");
        Status = FiscalDocumentStatus.Cancelled;
    }
}
