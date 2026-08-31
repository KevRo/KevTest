using KevTest.Api.GraphQL;
using KevTest.Core.Entities;
using KevTest.Data;
using KevTest.Data.Repositories;
using KevTest.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KevTest.Tests;

public class GraphQLQueryTests
{
    private static Query CreateQuery(out AppDbContext context, out ProductService productService)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        productService = new ProductService(new Repository<Product>(context));
        return new Query();
    }

    [Fact]
    public async Task GetProducts_ReturnsEmptyList_WhenNoProducts()
    {
        var query = CreateQuery(out _, out var productService);

        var result = await query.GetProducts(productService, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        var query = CreateQuery(out var context, out var productService);
        context.Products.AddRange(
            new Product { Name = "Widget", Price = 10m },
            new Product { Name = "Gadget", Price = 20m });
        context.SaveChanges();

        var result = await query.GetProducts(productService, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetProduct_ReturnsNull_WhenNotExists()
    {
        var query = CreateQuery(out _, out var productService);

        var result = await query.GetProduct(999, productService, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProduct_ReturnsProduct_WhenExists()
    {
        var query = CreateQuery(out var context, out var productService);
        var product = new Product { Name = "Test", Price = 15m };
        context.Products.Add(product);
        context.SaveChanges();

        var result = await query.GetProduct(product.Id, productService, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test", result!.Name);
        Assert.Equal(15m, result.Price);
    }
}
