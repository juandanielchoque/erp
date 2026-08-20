using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Orders;

public sealed class OrderLine : Entity
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    public OrderLine(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Line quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Line unit price cannot be negative.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
