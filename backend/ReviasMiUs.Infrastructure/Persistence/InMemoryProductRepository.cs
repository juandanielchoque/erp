using ReviasMiUs.Application.Abstractions;
using ReviasMiUs.Domain.Inventory;

namespace ReviasMiUs.Infrastructure.Persistence;

public sealed class InMemoryProductRepository(InMemoryErpStore store) : IProductRepository
{
    public IReadOnlyCollection<Product> List() => store.Products.ToArray();

    public Product? GetById(Guid id) => store.Products.FirstOrDefault(product => product.Id == id);

    public Product? GetBySku(string sku) =>
        store.Products.FirstOrDefault(product => string.Equals(product.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public void Add(Product product) => store.Products.Add(product);

    public void Update(Product product)
    {
        var index = store.Products.FindIndex(item => item.Id == product.Id);
        if (index >= 0)
        {
            store.Products[index] = product;
        }
    }
    public void Delete(Guid id) => store.Products.RemoveAll(item => item.Id == id);
}
