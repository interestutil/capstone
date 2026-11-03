using SmartWarehouse.Domain.Entities;

namespace SmartWarehouse.Infrastructure.Repositories;

public interface IWarehouseRepository
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetUserAsync(int id, CancellationToken ct);
    Task<List<User>> ListWorkersAsync(CancellationToken ct);
    Task<List<Branch>> ListBranchesAsync(CancellationToken ct);
    Task<List<Product>> ListProductsAsync(CancellationToken ct);
    Task<Product?> FindProductByBarcodeAsync(string code, CancellationToken ct);
    Task<Product?> GetProductAsync(int id, CancellationToken ct);
    Task<List<Order>> ListOrdersAsync(string? status, CancellationToken ct);
    Task<Order?> GetOrderAsync(int id, CancellationToken ct);
    void AddOrder(Order order);
    Task<List<InboundOrder>> ListInboundAsync(CancellationToken ct);
    Task<InboundOrder?> GetInboundAsync(int id, CancellationToken ct);
    void AddInbound(InboundOrder inbound);
    Task<List<MapNode>> ListNodesAsync(CancellationToken ct);
    Task<List<MapEdge>> ListEdgesAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task BeginTransactionAsync(CancellationToken ct);
    Task CommitTransactionAsync(CancellationToken ct);
    Task RollbackTransactionAsync(CancellationToken ct);
}
