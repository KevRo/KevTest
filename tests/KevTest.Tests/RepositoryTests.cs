using KevTest.Core.Entities;
using KevTest.Data;
using KevTest.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KevTest.Tests;

public class RepositoryTests
{
    private static Repository<Product> CreateRepository(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        return new Repository<Product>(context);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity_WhenExists()
    {
        var repo = CreateRepository(out var context);
        var product = new Product { Name = "Test", Price = 10m };
        await repo.AddAsync(product);
        await repo.SaveChangesAsync();

        var found = await repo.GetByIdAsync(product.Id);

        Assert.NotNull(found);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        var repo = CreateRepository(out _);

        var found = await repo.GetByIdAsync(999);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "A", Price = 1m });
        await repo.AddAsync(new Product { Name = "B", Price = 2m });
        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "Widget", Price = 10m });
        await repo.AddAsync(new Product { Name = "Gadget", Price = 5m });
        await repo.AddAsync(new Product { Name = "Widget Pro", Price = 20m });
        await repo.SaveChangesAsync();

        var widgets = await repo.FindAsync(p => p.Name.Contains("Widget"));

        Assert.Equal(2, widgets.Count);
        Assert.All(widgets, p => Assert.Contains("Widget", p.Name));
    }

    [Fact]
    public async Task FindAsync_ReturnsEmpty_WhenNoMatches()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "Widget", Price = 10m });
        await repo.SaveChangesAsync();

        var result = await repo.FindAsync(p => p.Name.Contains("NonExistent"));

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        var repo = CreateRepository(out var context);
        var product = new Product { Name = "New", Price = 15m };

        await repo.AddAsync(product);
        await repo.SaveChangesAsync();

        var found = await repo.GetByIdAsync(product.Id);
        Assert.NotNull(found);
        Assert.Equal("New", found.Name);
    }

    [Fact]
    public async Task Update_ModifiesEntity()
    {
        var repo = CreateRepository(out var context);
        var product = new Product { Name = "Original", Price = 10m };
        await repo.AddAsync(product);
        await repo.SaveChangesAsync();

        product.Name = "Updated";
        product.Price = 20m;
        repo.Update(product);
        await repo.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(product.Id);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(20m, updated.Price);
    }

    [Fact]
    public async Task Remove_DeletesEntity()
    {
        var repo = CreateRepository(out var context);
        var product = new Product { Name = "ToDelete", Price = 5m };
        await repo.AddAsync(product);
        await repo.SaveChangesAsync();

        repo.Remove(product);
        await repo.SaveChangesAsync();

        var found = await repo.GetByIdAsync(product.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task SaveChangesAsync_ReturnsNumberOfChanges()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "A", Price = 1m });
        await repo.AddAsync(new Product { Name = "B", Price = 2m });

        var changes = await repo.SaveChangesAsync();

        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotTrackEntities()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "Test", Price = 10m });
        await repo.SaveChangesAsync();

        var products = await repo.GetAllAsync();
        var firstProduct = products.First();

        // If entities are not tracked, modifying them won't be tracked by context
        firstProduct.Name = "Modified";
        var changes = await repo.SaveChangesAsync();

        // Should be 0 changes since entities are AsNoTracking
        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task FindAsync_DoesNotTrackEntities()
    {
        var repo = CreateRepository(out var context);
        await repo.AddAsync(new Product { Name = "Test", Price = 10m });
        await repo.SaveChangesAsync();

        var products = await repo.FindAsync(p => p.Name == "Test");
        var firstProduct = products.First();

        firstProduct.Name = "Modified";
        var changes = await repo.SaveChangesAsync();

        // Should be 0 changes since entities are AsNoTracking
        Assert.Equal(0, changes);
    }
}
