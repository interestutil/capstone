using Microsoft.EntityFrameworkCore;
using SmartWarehouse.Domain.Entities;

namespace SmartWarehouse.Infrastructure.Data;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<MapNode> MapNodes => Set<MapNode>();
    public DbSet<MapEdge> MapEdges => Set<MapEdge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.ToTable("Branches");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ProductType>(e =>
        {
            e.ToTable("ProductTypes");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.PieceBarcode).HasMaxLength(80).IsRequired();
            e.Property(x => x.CartonBarcode).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.PieceBarcode).IsUnique();
            e.HasIndex(x => x.CartonBarcode).IsUnique();
            e.HasOne(x => x.ProductType).WithMany(t => t.Products).HasForeignKey(x => x.ProductTypeId);
        });

        modelBuilder.Entity<Shelf>(e =>
        {
            e.ToTable("Shelves");
            e.Property(x => x.Code).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.LedDeviceId).HasMaxLength(80);
            e.Ignore(x => x.Available);
            e.Ignore(x => x.IsFull);
            e.Ignore(x => x.IsEmpty);
            e.HasOne(x => x.ProductType).WithMany(t => t.Shelves).HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Node).WithMany().HasForeignKey(x => x.NodeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders");
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.ToTable("OrderLines");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Shelf).WithMany().HasForeignKey(x => x.ShelfId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AssignedWorker).WithMany().HasForeignKey(x => x.AssignedWorkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InboundOrder>(e =>
        {
            e.ToTable("InboundOrders");
            e.Property(x => x.VoucherNumber).HasMaxLength(80).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetShelf).WithMany().HasForeignKey(x => x.TargetShelfId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByWorker).WithMany().HasForeignKey(x => x.CreatedByWorkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MapNode>(e =>
        {
            e.ToTable("MapNodes");
            e.Property(x => x.Type).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<MapEdge>(e =>
        {
            e.ToTable("MapEdges");
            e.HasOne(x => x.FromNode).WithMany().HasForeignKey(x => x.FromNodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToNode).WithMany().HasForeignKey(x => x.ToNodeId).OnDelete(DeleteBehavior.Restrict);
        });

        WarehouseSeed.ApplyCatalog(modelBuilder);
    }
}
