using KevTest.Core.Dtos;
using KevTest.Core.Entities;
using KevTest.Core.Interfaces;

namespace KevTest.Services;

public class ProductService : IProductService
{
    private readonly IRepository<Product> _products;

    public ProductService(IRepository<Product> products)
    {
        _products = products;
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ToDto(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _products.GetAllAsync(cancellationToken);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        await _products.AddAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _products.Remove(product);
        await _products.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductDto ToDto(Product product) => new(product.Id, product.Name, product.Price);
}
