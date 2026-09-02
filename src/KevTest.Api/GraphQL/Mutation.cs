using HotChocolate;
using KevTest.Core.Dtos;
using KevTest.Core.Interfaces;

namespace KevTest.Api.GraphQL;

public class Mutation
{
    public Task<ProductDto> CreateProduct(
        CreateProductDto input, [Service] IProductService productService, CancellationToken cancellationToken)
        => productService.CreateAsync(input, cancellationToken);

    public Task<bool> DeleteProduct(
        int id, [Service] IProductService productService, CancellationToken cancellationToken)
        => productService.DeleteAsync(id, cancellationToken);
}
