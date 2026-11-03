using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartWarehouse.Infrastructure.Data;

namespace SmartWarehouse.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(IsWarehouseContext).ToList())
                services.Remove(descriptor);

            services.AddDbContext<WarehouseDbContext>(options => options.UseSqlite(_connection));
        });
    }

    private static bool IsWarehouseContext(ServiceDescriptor descriptor) =>
        descriptor.ServiceType == typeof(WarehouseDbContext) ||
        descriptor.ServiceType == typeof(DbContextOptions<WarehouseDbContext>) ||
        descriptor.ServiceType == typeof(DbContextOptions) ||
        (descriptor.ImplementationType?.Name.Contains("WarehouseDbContext", StringComparison.Ordinal) ?? false);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection?.Dispose();
        base.Dispose(disposing);
    }
}
