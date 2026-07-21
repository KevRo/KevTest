using KevTest.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KevTest.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers business services plus the typed HttpClient backing IExternalApiClient.
    /// Set ExternalApi:BaseUrl in configuration to point it at a real API.
    /// </summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductService, ProductService>();

        services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
        {
            var baseUrl = configuration["ExternalApi:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        });

        return services;
    }
}
