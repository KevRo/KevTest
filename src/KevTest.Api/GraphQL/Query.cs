using HotChocolate;
using KevTest.Core.Dtos;
using KevTest.Core.Interfaces;

namespace KevTest.Api.GraphQL;

public class Query
{
    public Task<IReadOnlyList<ProductDto>> GetProducts(
        [Service] IProductService productService, CancellationToken cancellationToken)
        => productService.GetAllAsync(cancellationToken);

    public Task<ProductDto?> GetProduct(
        int id, [Service] IProductService productService, CancellationToken cancellationToken)
        => productService.GetByIdAsync(id, cancellationToken);
}
