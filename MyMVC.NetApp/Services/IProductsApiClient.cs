using KevTest.Core.Dtos;
using KevTest.Core.Enums;

namespace MyMVC.NetApp.Services;

public interface IProductsApiClient
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        ProductSortField? sortBy = null, bool descending = false, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
