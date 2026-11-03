using Microsoft.EntityFrameworkCore;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Data;

namespace SmartWarehouse.Infrastructure.Repositories;

public sealed class ShelfRepository(WarehouseDbContext db) : IShelfRepository
{
    public Task<Shelf?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Shelves
            .Include(s => s.ProductType)
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<Shelf>> ListAsync(CancellationToken ct) =>
        db.Shelves
            .AsNoTracking()
            .Include(s => s.ProductType)
            .Include(s => s.Product)
            .OrderBy(s => s.Rack)
            .ThenBy(s => s.Level)
            .ToListAsync(ct);

    public async Task<bool> TryIncreaseCommittedAsync(int shelfId, int qty, CancellationToken ct)
    {
        var rows = await db.Shelves
            .Where(s => s.Id == shelfId && s.OnHand - s.Committed >= qty)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Committed, x => x.Committed + qty), ct);
        return rows == 1;
    }

    public async Task<bool> TryDecreaseCommittedAsync(int shelfId, int qty, CancellationToken ct)
    {
        var rows = await db.Shelves
            .Where(s => s.Id == shelfId && s.Committed >= qty)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Committed, x => x.Committed - qty), ct);
        return rows == 1;
    }

    public async Task<bool> TryConfirmPickAsync(int shelfId, int qty, CancellationToken ct)
    {
        var rows = await db.Shelves
            .Where(s => s.Id == shelfId && s.OnHand >= qty && s.Committed >= qty)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.OnHand, x => x.OnHand - qty)
                .SetProperty(x => x.Committed, x => x.Committed - qty), ct);
        if (rows != 1)
            return false;

        await db.Shelves
            .Where(s => s.Id == shelfId && s.OnHand == 0 && s.Committed == 0)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProductId, (int?)null), ct);
        return true;
    }

    public async Task<bool> TryPlaceInboundAsync(int shelfId, int productId, int qty, CancellationToken ct)
    {
        var rows = await db.Shelves
            .Where(s => s.Id == shelfId
                        && s.OnHand + qty <= s.MaxCapacity
                        && (s.ProductId == null || s.ProductId == productId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.OnHand, x => x.OnHand + qty)
                .SetProperty(x => x.ProductId, productId), ct);
        return rows == 1;
    }
}
