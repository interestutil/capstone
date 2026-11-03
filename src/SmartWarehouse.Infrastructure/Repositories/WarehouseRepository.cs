using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartWarehouse.Domain;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Data;

namespace SmartWarehouse.Infrastructure.Repositories;

public sealed class WarehouseRepository(WarehouseDbContext db) : IWarehouseRepository
{
    private IDbContextTransaction? _transaction;

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.ToLower();
        return db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, ct);
    }

    public Task<User?> GetUserAsync(int id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<List<User>> ListWorkersAsync(CancellationToken ct) =>
        db.Users.AsNoTracking()
            .Where(u => u.Role == UserRoles.Worker)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

    public Task<List<Branch>> ListBranchesAsync(CancellationToken ct) =>
        db.Branches.AsNoTracking().OrderBy(b => b.Name).ToListAsync(ct);

    public Task<List<Product>> ListProductsAsync(CancellationToken ct) =>
        db.Products.AsNoTracking().Include(p => p.ProductType).OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Product?> FindProductByBarcodeAsync(string code, CancellationToken ct)
    {
        var normalized = code.ToLower();
        return db.Products.Include(p => p.ProductType).FirstOrDefaultAsync(
            p => p.PieceBarcode.ToLower() == normalized || p.CartonBarcode.ToLower() == normalized, ct);
    }

    public Task<Product?> GetProductAsync(int id, CancellationToken ct) =>
        db.Products.Include(p => p.ProductType).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<List<Order>> ListOrdersAsync(string? status, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Branch).Include(o => o.Lines).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);
        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    }

    public Task<Order?> GetOrderAsync(int id, CancellationToken ct) =>
        db.Orders
            .Include(o => o.Branch)
            .Include(o => o.Lines).ThenInclude(l => l.Shelf)
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Lines).ThenInclude(l => l.AssignedWorker)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public void AddOrder(Order order) => db.Orders.Add(order);

    public Task<List<InboundOrder>> ListInboundAsync(CancellationToken ct) =>
        db.InboundOrders.AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.TargetShelf)
            .Include(i => i.CreatedByWorker)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public Task<InboundOrder?> GetInboundAsync(int id, CancellationToken ct) =>
        db.InboundOrders
            .Include(i => i.Product)
            .Include(i => i.TargetShelf)
            .Include(i => i.CreatedByWorker)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public void AddInbound(InboundOrder inbound) => db.InboundOrders.Add(inbound);

    public Task<List<MapNode>> ListNodesAsync(CancellationToken ct) =>
        db.MapNodes.AsNoTracking().ToListAsync(ct);

    public Task<List<MapEdge>> ListEdgesAsync(CancellationToken ct) =>
        db.MapEdges.AsNoTracking().ToListAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct) =>
        _transaction = await db.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
