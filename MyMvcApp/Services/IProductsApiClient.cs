using KevTest.Core.Dtos;

namespace MyMvcApp.Services;

public interface IProductsApiClient
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
