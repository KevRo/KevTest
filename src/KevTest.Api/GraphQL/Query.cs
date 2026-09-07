using HotChocolate;
using KevTest.Core.Dtos;
using KevTest.Core.Enums;
using KevTest.Core.Interfaces;

namespace KevTest.Api.GraphQL;

public class Query
{
    public async Task<IReadOnlyList<ProductDto>> GetProducts(
        [Service] IProductService productService,
        CancellationToken cancellationToken,
        ProductSortField? sortBy = null,
        bool descending = false)
    {
        var products = await productService.GetAllAsync(cancellationToken);

        IEnumerable<ProductDto> sorted = sortBy switch
        {
            ProductSortField.Name => descending
                ? products.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase)
                : products.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase),
            ProductSortField.Price => descending
                ? products.OrderByDescending(p => p.Price)
                : products.OrderBy(p => p.Price),
            _ => products
        };

        return sorted.ToList();
    }

    public Task<ProductDto?> GetProduct(
        int id, [Service] IProductService productService, CancellationToken cancellationToken)
        => productService.GetByIdAsync(id, cancellationToken);
}
