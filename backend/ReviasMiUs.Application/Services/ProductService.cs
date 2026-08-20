using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Application.Dtos;
using ReviasMiUs.Domain.Inventory;
using ReviasMiUs.Domain.Common;

namespace ReviasMiUs.Application.Services;

public sealed class ProductService(IProductRepository products, ISalesOrderRepository? orders = null)
{
    public ProductDto Create(CreateProductRequest request)
    {
        EnsureUniqueSku(request.Sku);
        var product = new Product(request.Name, request.Sku, request.UnitPrice, request.StockQuantity, request.ReorderPoint, request.Category);
        products.Add(product);
        return Map(product);
    }

    public IReadOnlyCollection<ProductDto> List() =>
        products.List()
            .OrderBy(product => product.Name)
            .Select(Map)
            .ToArray();

    public IReadOnlyCollection<ProductDto> LowStock() =>
        products.List()
            .Where(product => product.NeedsReorder())
            .OrderBy(product => product.Sku)
            .Select(Map)
            .ToArray();

    public Product? FindById(Guid id) => products.GetById(id);

    public ProductDto Update(Guid id, UpdateProductRequest request)
    {
        var product = products.GetById(id) ?? throw new DomainException("Product not found.");
        EnsureUniqueSku(request.Sku, id);
        product.UpdateDetails(request.Name, request.Sku, request.UnitPrice, request.StockQuantity, request.ReorderPoint, request.Category);
        if (request.IsActive) product.Activate(); else product.Deactivate();
        products.Update(product);
        return Map(product);
    }

    public void Delete(Guid id)
    {
        _ = products.GetById(id) ?? throw new DomainException("Product not found.");
        if (orders?.List().Any(order => order.Lines.Any(line => line.ProductId == id)) == true)
            throw new DomainException("Products with sales history cannot be deleted; deactivate them instead.");
        products.Delete(id);
    }

    private void EnsureUniqueSku(string sku, Guid? excludedId = null)
    {
        var existing = products.GetBySku(sku);
        if (existing is not null && existing.Id != excludedId) throw new DomainException("A product with this SKU already exists.");
    }

    private static ProductDto Map(Product product) =>
        new(product.Id, product.Name, product.Sku, product.Category, product.UnitPrice, product.StockQuantity, product.ReorderPoint, product.NeedsReorder(), product.IsActive);
}
