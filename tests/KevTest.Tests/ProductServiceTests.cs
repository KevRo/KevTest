using KevTest.Core.Dtos;
using KevTest.Data;
using KevTest.Data.Repositories;
using KevTest.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KevTest.Tests;

public class ProductServiceTests
{
    private static ProductService CreateService(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        var repository = new Repository<Core.Entities.Product>(context);
        return new ProductService(repository);
    }

    [Fact]
    public async Task CreateAsync_PersistsProduct_AndReturnsDto()
    {
        var service = CreateService(out _);

        var created = await service.CreateAsync(new CreateProductDto("Widget", 9.99m));

        Assert.True(created.Id > 0);
        Assert.Equal("Widget", created.Name);
        Assert.Equal(9.99m, created.Price);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenProductDoesNotExist()
    {
        var service = CreateService(out _);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCreatedProducts()
    {
        var service = CreateService(out _);
        await service.CreateAsync(new CreateProductDto("A", 1m));
        await service.CreateAsync(new CreateProductDto("B", 2m));

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProduct_AndReturnsTrue()
    {
        var service = CreateService(out _);
        var created = await service.CreateAsync(new CreateProductDto("ToDelete", 5m));

        var deleted = await service.DeleteAsync(created.Id);
        var afterDelete = await service.GetByIdAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenProductDoesNotExist()
    {
        var service = CreateService(out _);

        var deleted = await service.DeleteAsync(123);

        Assert.False(deleted);
    }
}
