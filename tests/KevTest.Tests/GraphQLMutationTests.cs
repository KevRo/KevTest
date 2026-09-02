using KevTest.Api.GraphQL;
using KevTest.Core.Dtos;
using KevTest.Core.Entities;
using KevTest.Data;
using KevTest.Data.Repositories;
using KevTest.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KevTest.Tests;

public class GraphQLMutationTests
{
    private static Mutation CreateMutation(out AppDbContext context, out ProductService productService)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        productService = new ProductService(new Repository<Product>(context));
        return new Mutation();
    }

    [Fact]
    public async Task CreateProduct_AddsAndReturnsProduct()
    {
        var mutation = CreateMutation(out var context, out var productService);
        var input = new CreateProductDto("New Product", 25m);

        var result = await mutation.CreateProduct(input, productService, CancellationToken.None);

        Assert.Equal("New Product", result.Name);
        Assert.Equal(25m, result.Price);
        Assert.True(result.Id > 0);
        Assert.Single(context.Products);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsFalse_WhenNotExists()
    {
        var mutation = CreateMutation(out _, out var productService);

        var result = await mutation.DeleteProduct(999, productService, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsTrue_AndRemovesProduct_WhenExists()
    {
        var mutation = CreateMutation(out var context, out var productService);
        var product = new Product { Name = "ToDelete", Price = 5m };
        context.Products.Add(product);
        context.SaveChanges();

        var result = await mutation.DeleteProduct(product.Id, productService, CancellationToken.None);

        Assert.True(result);
        Assert.Empty(context.Products);
    }
}
