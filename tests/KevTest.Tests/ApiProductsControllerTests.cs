using KevTest.Api.Controllers;
using KevTest.Core.Dtos;
using KevTest.Core.Interfaces;
using KevTest.Data;
using KevTest.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KevTest.Tests;

public class ApiProductsControllerTests
{
    private static ProductsController CreateController(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        var repository = new Data.Repositories.Repository<Core.Entities.Product>(context);
        var productService = new ProductService(repository);
        return new ProductsController(productService);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEmptyList()
    {
        var controller = CreateController(out _);

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductDto>>(okResult.Value);
        Assert.Empty(products);
    }

    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        var controller = CreateController(out var context);
        context.Products.AddRange(
            new Core.Entities.Product { Name = "Widget", Price = 10m },
            new Core.Entities.Product { Name = "Gadget", Price = 20m }
        );
        context.SaveChanges();

        var result = await controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IReadOnlyList<ProductDto>>(okResult.Value);
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNotExists()
    {
        var controller = CreateController(out _);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_ReturnsProduct_WhenExists()
    {
        var controller = CreateController(out var context);
        var product = new Core.Entities.Product { Name = "Test", Price = 15m };
        context.Products.Add(product);
        context.SaveChanges();

        var result = await controller.GetById(product.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal("Test", dto.Name);
        Assert.Equal(15m, dto.Price);
    }

    [Fact]
    public async Task Create_ReturnsCreatedProduct()
    {
        var controller = CreateController(out _);
        var request = new CreateProductDto("New Product", 25m);

        var result = await controller.Create(request, CancellationToken.None);

        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ProductDto>(createdAtResult.Value);
        Assert.Equal("New Product", dto.Name);
        Assert.Equal(25m, dto.Price);
        Assert.True(dto.Id > 0);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNotExists()
    {
        var controller = CreateController(out _);

        var result = await controller.Delete(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenExists()
    {
        var controller = CreateController(out var context);
        var product = new Core.Entities.Product { Name = "ToDelete", Price = 5m };
        context.Products.Add(product);
        context.SaveChanges();

        var result = await controller.Delete(product.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        // Verify it's actually deleted
        var getResult = await controller.GetById(product.Id, CancellationToken.None);
        Assert.IsType<NotFoundResult>(getResult.Result);
    }
}
