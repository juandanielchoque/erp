using ReviasMiUs.Domain.Common;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Operations;
using ReviasMiUs.Domain.Orders;

namespace ReviasMiUs.Tests;

public sealed class ManagementRulesTests
{
    [Fact]
    public void Product_UpdateDetailsChangesCatalogAndStockData()
    {
        var product = new Product("Producto", "SKU-1", 10m, 5, 2);

        product.UpdateDetails("Producto editado", "SKU-2", 12.5m, 9, 3, "Fondos");

        Assert.Equal("Producto editado", product.Name);
        Assert.Equal("SKU-2", product.Sku);
        Assert.Equal(12.5m, product.UnitPrice);
        Assert.Equal(9, product.StockQuantity);
        Assert.Equal("Fondos", product.Category);
    }

    [Fact]
    public void SalesOrder_UpdateDraftReplacesHeaderAndLines()
    {
        var order = new SalesOrder(Guid.NewGuid(), "S-QA", DateTime.UtcNow.AddDays(2));
        order.AddLine(new OrderLine(Guid.NewGuid(), 1, 10m));
        var replacementProductId = Guid.NewGuid();

        order.UpdateDraft(
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(3),
            "Editada",
            OrderServiceType.Delivery,
            null,
            [new OrderLine(replacementProductId, 3, 15m)]);

        Assert.Single(order.Lines);
        Assert.Equal(replacementProductId, order.Lines.Single().ProductId);
        Assert.Equal(45m, order.TotalAmount);
        Assert.Equal(OrderServiceType.Delivery, order.ServiceType);
    }

    [Fact]
    public void RestaurantTable_CannotBeEditedWhileOccupied()
    {
        var table = new RestaurantTable("Mesa 01", "Salon", 4);
        table.Occupy(Guid.NewGuid());

        Assert.Throws<DomainException>(() => table.UpdateDetails("Mesa 02", "Terraza", 6));
    }

    [Fact]
    public void FiscalDocument_CancelPreservesDocumentAndChangesStatus()
    {
        var document = new FiscalDocument(Guid.NewGuid(), FiscalDocumentType.Receipt, "B001-0001", "Cliente", null, 20m);

        document.Cancel();

        Assert.Equal(FiscalDocumentStatus.Cancelled, document.Status);
        Assert.Equal("B001-0001", document.Number);
    }
}
