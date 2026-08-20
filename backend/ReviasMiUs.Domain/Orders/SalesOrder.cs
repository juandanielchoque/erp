using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Orders;

public enum SalesOrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Cancelled = 2
}

public enum OrderServiceType
{
    Retail = 0,
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3
}

public sealed class SalesOrder : Entity
{
    private readonly List<OrderLine> _lines = [];

    public string OrderNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public SalesOrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ValidUntilUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public OrderServiceType ServiceType { get; private set; }
    public string? TableNumber { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public decimal TotalAmount => _lines.Sum(line => line.LineTotal);

    public SalesOrder(
        Guid customerId,
        string orderNumber,
        DateTime validUntilUtc,
        string? notes = null,
        OrderServiceType serviceType = OrderServiceType.Retail,
        string? tableNumber = null)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number is required.");
        }

        if (validUntilUtc <= DateTime.UtcNow)
        {
            throw new DomainException("Quotation validity date must be in the future.");
        }

        if (serviceType == OrderServiceType.DineIn && string.IsNullOrWhiteSpace(tableNumber))
        {
            throw new DomainException("A table number is required for dine-in orders.");
        }

        CustomerId = customerId;
        OrderNumber = orderNumber.Trim();
        Status = SalesOrderStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        ValidUntilUtc = validUntilUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ServiceType = serviceType;
        TableNumber = string.IsNullOrWhiteSpace(tableNumber) ? null : tableNumber.Trim();
    }

    public void AddLine(OrderLine line)
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be edited.");
        }

        _lines.Add(line);
    }

    public void UpdateDraft(Guid customerId, DateTime validUntilUtc, string? notes, OrderServiceType serviceType, string? tableNumber, IEnumerable<OrderLine> lines)
    {
        if (Status != SalesOrderStatus.Draft) throw new DomainException("Only draft orders can be edited.");
        if (customerId == Guid.Empty || validUntilUtc <= DateTime.UtcNow)
            throw new DomainException("Customer and a future validity date are required.");
        if (serviceType == OrderServiceType.DineIn && string.IsNullOrWhiteSpace(tableNumber))
            throw new DomainException("A table number is required for dine-in orders.");
        var updatedLines = lines.ToArray();
        if (updatedLines.Length == 0) throw new DomainException("An order must have at least one line.");

        CustomerId = customerId;
        ValidUntilUtc = validUntilUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ServiceType = serviceType;
        TableNumber = string.IsNullOrWhiteSpace(tableNumber) ? null : tableNumber.Trim();
        _lines.Clear();
        _lines.AddRange(updatedLines);
    }

    public void Confirm()
    {
        if (Status != SalesOrderStatus.Draft)
        {
            throw new DomainException("Only draft quotations can be confirmed.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("An order must have at least one line.");
        }

        if (ValidUntilUtc <= DateTime.UtcNow)
        {
            throw new DomainException("The quotation has expired.");
        }

        Status = SalesOrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SalesOrderStatus.Cancelled)
        {
            throw new DomainException("The sales document is already cancelled.");
        }

        Status = SalesOrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }
}
