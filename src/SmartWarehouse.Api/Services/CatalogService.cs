using SmartWarehouse.Api.Errors;
using SmartWarehouse.Api.Mapping;
using SmartWarehouse.Contracts;
using SmartWarehouse.Infrastructure.Repositories;

namespace SmartWarehouse.Api.Services;

public sealed class CatalogService(IWarehouseRepository repo, IShelfRepository shelves)
{
    public async Task<List<BranchDto>> GetBranchesAsync(CancellationToken ct) =>
        (await repo.ListBranchesAsync(ct)).Select(b => b.ToDto()).ToList();

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken ct) =>
        (await repo.ListProductsAsync(ct)).Select(p => p.ToDto()).ToList();

    public async Task<ProductDto> GetProductByBarcodeAsync(string code, CancellationToken ct)
    {
        var product = await repo.FindProductByBarcodeAsync(code.Trim(), ct);
        if (product is null)
            throw WarehouseException.NotFound("Unknown barcode.");
        return product.ToDto();
    }

    public async Task<List<ShelfDto>> GetShelvesAsync(CancellationToken ct) =>
        (await shelves.ListAsync(ct)).Select(s => s.ToDto()).ToList();

    public async Task<List<InventoryRowDto>> GetInventoryAsync(CancellationToken ct)
    {
        var products = await repo.ListProductsAsync(ct);
        var shelfList = await shelves.ListAsync(ct);
        return products.Select(p =>
        {
            var holding = shelfList.Where(s => s.ProductId == p.Id).ToList();
            var onHand = holding.Sum(s => s.OnHand);
            var committed = holding.Sum(s => s.Committed);
            return new InventoryRowDto(p.Id, p.Name, onHand, committed, onHand - committed, holding.Count);
        }).ToList();
    }

    public async Task<List<WorkerDto>> GetWorkersAsync(CancellationToken ct) =>
        (await repo.ListWorkersAsync(ct)).Select(u => u.ToWorkerDto()).ToList();
}
