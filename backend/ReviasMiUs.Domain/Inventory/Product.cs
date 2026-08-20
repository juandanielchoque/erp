using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Domain.Inventory;

public sealed class Product : Entity
{
    public string Name { get; private set; }
    public string Sku { get; private set; }
    public string Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int ReorderPoint { get; private set; }
    public bool IsActive { get; private set; }

    public Product(string name, string sku, decimal unitPrice, int stockQuantity, int reorderPoint, string category = "General")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Product SKU is required.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Product price cannot be negative.");
        }

        if (stockQuantity < 0)
        {
            throw new DomainException("Stock quantity cannot be negative.");
        }

        if (reorderPoint < 0)
        {
            throw new DomainException("Reorder point cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new DomainException("Product category is required.");
        }

        Name = name.Trim();
        Sku = sku.Trim().ToUpperInvariant();
        Category = category.Trim();
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        ReorderPoint = reorderPoint;
        IsActive = true;
    }

    public void AdjustStock(int quantityDelta)
    {
        var nextQuantity = StockQuantity + quantityDelta;
        if (nextQuantity < 0)
        {
            throw new DomainException($"Not enough stock for product {Sku}.");
        }

        StockQuantity = nextQuantity;
    }

    public bool NeedsReorder() => IsActive && StockQuantity <= ReorderPoint;

    public void UpdateDetails(string name, string sku, decimal unitPrice, int stockQuantity, int reorderPoint, string category)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(category))
            throw new DomainException("Product name, SKU and category are required.");
        if (unitPrice < 0 || stockQuantity < 0 || reorderPoint < 0)
            throw new DomainException("Price, stock and reorder point cannot be negative.");
        Name = name.Trim();
        Sku = sku.Trim().ToUpperInvariant();
        Category = category.Trim();
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        ReorderPoint = reorderPoint;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
