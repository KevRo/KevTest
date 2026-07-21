using KevTest.Core.Interfaces;
using KevTest.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KevTest.Data;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DbContext (SQLite by default) and the generic repository.
    /// Swap the provider here (e.g. UseSqlServer/UseNpgsql) without touching callers.
    /// </summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=kevtest.db";

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
