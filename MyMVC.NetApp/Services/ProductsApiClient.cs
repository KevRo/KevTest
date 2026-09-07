using System.Net.Http.Json;
using System.Text.Json;
using KevTest.Core.Dtos;
using KevTest.Core.Enums;

namespace MyMVC.NetApp.Services;

public class ProductsApiClient : IProductsApiClient
{
    private static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public ProductsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        ProductSortField? sortBy = null, bool descending = false, CancellationToken cancellationToken = default)
    {
        var sortArg = sortBy switch
        {
            ProductSortField.Name => "NAME",
            ProductSortField.Price => "PRICE",
            _ => null
        };

        var query = sortArg is null
            ? "query { products { id name price } }"
            : $"query {{ products(sortBy: {sortArg}, descending: {(descending ? "true" : "false")}) {{ id name price }} }}";

        var response = await _httpClient.PostAsJsonAsync("graphql", new { query }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphQlProductsResponse>(
            ResponseOptions, cancellationToken);

        return result?.Data?.Products ?? new List<ProductDto>();
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/products/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private sealed record GraphQlProductsResponse(GraphQlProductsData? Data);

    private sealed record GraphQlProductsData(List<ProductDto> Products);
}
