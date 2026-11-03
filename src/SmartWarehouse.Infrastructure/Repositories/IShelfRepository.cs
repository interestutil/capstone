using SmartWarehouse.Domain.Entities;

namespace SmartWarehouse.Infrastructure.Repositories;

public interface IShelfRepository
{
    Task<Shelf?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<Shelf>> ListAsync(CancellationToken ct);
    Task<bool> TryIncreaseCommittedAsync(int shelfId, int qty, CancellationToken ct);
    Task<bool> TryDecreaseCommittedAsync(int shelfId, int qty, CancellationToken ct);
    Task<bool> TryConfirmPickAsync(int shelfId, int qty, CancellationToken ct);
    Task<bool> TryPlaceInboundAsync(int shelfId, int productId, int qty, CancellationToken ct);
}
