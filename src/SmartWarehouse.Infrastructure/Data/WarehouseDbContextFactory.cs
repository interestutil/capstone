using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartWarehouse.Infrastructure.Data;

public sealed class WarehouseDbContextFactory : IDesignTimeDbContextFactory<WarehouseDbContext>
{
    public WarehouseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SmartWarehouse;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new WarehouseDbContext(options);
    }
}
