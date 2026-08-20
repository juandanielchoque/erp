using ReviasMiUs.Domain.Inventory;

namespace ReviasMiUs.Application.Abstractions;

public interface IProductRepository
{
    IReadOnlyCollection<Product> List();
    Product? GetById(Guid id);
    Product? GetBySku(string sku);
    void Add(Product product);
    void Update(Product product);
    void Delete(Guid id);
}
