using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartWarehouse.Domain;
using SmartWarehouse.Domain.Entities;

namespace SmartWarehouse.Infrastructure.Data;

public static class WarehouseSeed
{
    public const string AdminEmail = "admin@warehouse.local";
    public const string AdminPassword = "Admin123!";
    public const string WorkerPassword = "Worker123!";

    public static void ApplyCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>().HasData(
            new Branch { Id = 1, Name = "Smouha" },
            new Branch { Id = 2, Name = "El Asafra" });

        modelBuilder.Entity<ProductType>().HasData(
            new ProductType { Id = 1, Name = "Cleaning supplies" },
            new ProductType { Id = 2, Name = "Canned food" });

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laundry detergent", ProductTypeId = 1, PieceBarcode = "P-1001", CartonBarcode = "C-1001" },
            new Product { Id = 2, Name = "Floor bleach", ProductTypeId = 1, PieceBarcode = "P-1002", CartonBarcode = "C-1002" },
            new Product { Id = 3, Name = "Canned beans", ProductTypeId = 2, PieceBarcode = "P-2001", CartonBarcode = "C-2001" },
            new Product { Id = 4, Name = "Canned tomatoes", ProductTypeId = 2, PieceBarcode = "P-2002", CartonBarcode = "C-2002" });

        modelBuilder.Entity<MapNode>().HasData(
            new MapNode { Id = 1, X = 80, Y = 440, Type = MapNodeTypes.Dock },
            new MapNode { Id = 2, X = 80, Y = 200, Type = MapNodeTypes.Aisle },
            new MapNode { Id = 3, X = 400, Y = 200, Type = MapNodeTypes.Aisle },
            new MapNode { Id = 4, X = 720, Y = 200, Type = MapNodeTypes.Aisle },
            new MapNode { Id = 5, X = 200, Y = 120, Type = MapNodeTypes.ShelfAnchor },
            new MapNode { Id = 6, X = 320, Y = 120, Type = MapNodeTypes.ShelfAnchor },
            new MapNode { Id = 7, X = 480, Y = 120, Type = MapNodeTypes.ShelfAnchor },
            new MapNode { Id = 8, X = 600, Y = 120, Type = MapNodeTypes.ShelfAnchor },
            new MapNode { Id = 9, X = 720, Y = 120, Type = MapNodeTypes.ShelfAnchor },
            new MapNode { Id = 10, X = 200, Y = 280, Type = MapNodeTypes.ShelfAnchor });

        modelBuilder.Entity<MapEdge>().HasData(
            new MapEdge { Id = 1, FromNodeId = 1, ToNodeId = 2 },
            new MapEdge { Id = 2, FromNodeId = 2, ToNodeId = 3 },
            new MapEdge { Id = 3, FromNodeId = 3, ToNodeId = 4 },
            new MapEdge { Id = 4, FromNodeId = 2, ToNodeId = 5 },
            new MapEdge { Id = 5, FromNodeId = 3, ToNodeId = 6 },
            new MapEdge { Id = 6, FromNodeId = 3, ToNodeId = 7 },
            new MapEdge { Id = 7, FromNodeId = 4, ToNodeId = 8 },
            new MapEdge { Id = 8, FromNodeId = 4, ToNodeId = 9 },
            new MapEdge { Id = 9, FromNodeId = 2, ToNodeId = 10 });

        modelBuilder.Entity<Shelf>().HasData(
            new Shelf { Id = 1, Code = "R1-L1", Rack = 1, Level = 1, ProductTypeId = 1, ProductId = 1, MaxCapacity = 24, OnHand = 20, Committed = 0, NodeId = 5, LedDeviceId = "esp32-1", LedIndex = 0 },
            new Shelf { Id = 2, Code = "R1-L2", Rack = 1, Level = 2, ProductTypeId = 1, ProductId = null, MaxCapacity = 24, OnHand = 0, Committed = 0, NodeId = 6, LedDeviceId = "esp32-1", LedIndex = 1 },
            new Shelf { Id = 3, Code = "R2-L1", Rack = 2, Level = 1, ProductTypeId = 2, ProductId = 3, MaxCapacity = 24, OnHand = 18, Committed = 0, NodeId = 7, LedDeviceId = "esp32-1", LedIndex = 2 },
            new Shelf { Id = 4, Code = "R2-L2", Rack = 2, Level = 2, ProductTypeId = 2, ProductId = null, MaxCapacity = 24, OnHand = 0, Committed = 0, NodeId = 8, LedDeviceId = "esp32-1", LedIndex = 3 },
            new Shelf { Id = 5, Code = "R2-L3", Rack = 2, Level = 3, ProductTypeId = 2, ProductId = 4, MaxCapacity = 24, OnHand = 10, Committed = 0, NodeId = 9, LedDeviceId = "esp32-1", LedIndex = 4 },
            new Shelf { Id = 6, Code = "R3-L1", Rack = 3, Level = 1, ProductTypeId = 1, ProductId = 2, MaxCapacity = 16, OnHand = 8, Committed = 0, NodeId = 10, LedDeviceId = "esp32-1", LedIndex = 5 });
    }

    public static async Task EnsureUsersAsync(WarehouseDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Email = AdminEmail,
            FullName = "Nour Hassan",
            Role = UserRoles.Admin,
            IsActive = true
        };
        admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);

        var ahmed = new User
        {
            Email = "ahmed@warehouse.local",
            FullName = "Ahmed Farouk",
            Role = UserRoles.Worker,
            IsActive = true
        };
        ahmed.PasswordHash = hasher.HashPassword(ahmed, WorkerPassword);

        var sara = new User
        {
            Email = "sara@warehouse.local",
            FullName = "Sara Adel",
            Role = UserRoles.Worker,
            IsActive = true
        };
        sara.PasswordHash = hasher.HashPassword(sara, WorkerPassword);

        db.Users.AddRange(admin, ahmed, sara);
        await db.SaveChangesAsync(ct);
    }
}
