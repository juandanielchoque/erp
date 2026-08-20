namespace ReviasMiUs.Application.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    string Category,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderPoint,
    bool NeedsReorder,
    bool IsActive);

public sealed record CreateProductRequest(
    string Name,
    string Sku,
    decimal UnitPrice,
    int StockQuantity,
    int ReorderPoint,
    string Category = "General");
public sealed record UpdateProductRequest(string Name, string Sku, decimal UnitPrice, int StockQuantity, int ReorderPoint, string Category, bool IsActive = true);
