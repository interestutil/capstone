using SmartWarehouse.Api.Errors;
using SmartWarehouse.Api.Mapping;
using SmartWarehouse.Contracts;
using SmartWarehouse.Domain;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Repositories;

namespace SmartWarehouse.Api.Services;

public sealed class InboundService(IWarehouseRepository repo, IShelfRepository shelves)
{
    public async Task<InboundDto> CreateAsync(int workerId, CreateInboundRequest request, CancellationToken ct)
    {
        var product = await repo.FindProductByBarcodeAsync(request.Barcode.Trim(), ct)
                      ?? throw WarehouseException.NotFound("Unknown barcode.");
        if (request.QtyCartons <= 0)
            throw WarehouseException.BadRequest("Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.VoucherNumber))
            throw WarehouseException.BadRequest("Voucher number is required.");

        var inbound = new InboundOrder
        {
            VoucherNumber = request.VoucherNumber.Trim(),
            ProductId = product.Id,
            QtyCartons = request.QtyCartons,
            Status = InboundStatuses.Received,
            CreatedByWorkerId = workerId,
            CreatedAt = DateTime.UtcNow
        };
        repo.AddInbound(inbound);
        await repo.SaveChangesAsync(ct);
        return (await repo.GetInboundAsync(inbound.Id, ct))!.ToDto();
    }

    public async Task<List<InboundDto>> ListAsync(CancellationToken ct) =>
        (await repo.ListInboundAsync(ct)).Select(i => i.ToDto()).ToList();

    public async Task<InboundDto> AssignShelfAsync(int inboundId, int shelfId, CancellationToken ct)
    {
        var inbound = await repo.GetInboundAsync(inboundId, ct)
                      ?? throw WarehouseException.NotFound("Unknown inbound record.");
        if (inbound.Status == InboundStatuses.Placed)
            throw WarehouseException.Conflict("This inbound stock has already been placed.");

        var shelf = await shelves.GetByIdAsync(shelfId, ct)
                    ?? throw WarehouseException.NotFound("Unknown shelf.");
        var product = inbound.Product;
        if (shelf.OnHand >= shelf.MaxCapacity)
            throw WarehouseException.Conflict($"Shelf {shelf.Code} is full.", shelf.Id);
        if (shelf.ProductId is not null && shelf.ProductId != product.Id)
            throw WarehouseException.Conflict($"Shelf {shelf.Code} already holds another product.", shelf.Id);
        if (shelf.OnHand + inbound.QtyCartons > shelf.MaxCapacity)
        {
            throw WarehouseException.Conflict(
                $"Shelf {shelf.Code} does not have room for {inbound.QtyCartons} cartons.",
                shelf.Id, inbound.QtyCartons, shelf.MaxCapacity - shelf.OnHand);
        }

        inbound.TargetShelfId = shelf.Id;
        inbound.Status = InboundStatuses.ShelfAssigned;
        await repo.SaveChangesAsync(ct);
        return (await repo.GetInboundAsync(inbound.Id, ct))!.ToDto();
    }

    public async Task<InboundDto> PlaceAsync(int inboundId, int workerId, CancellationToken ct)
    {
        var inbound = await repo.GetInboundAsync(inboundId, ct)
                      ?? throw WarehouseException.NotFound("Unknown inbound record.");
        if (inbound.Status != InboundStatuses.ShelfAssigned || inbound.TargetShelfId is null)
            throw WarehouseException.Conflict("The admin has not assigned a shelf yet.");

        var shelf = await shelves.GetByIdAsync(inbound.TargetShelfId.Value, ct)
                    ?? throw WarehouseException.NotFound("Unknown shelf.");
        if (shelf.ProductId is not null && shelf.ProductId != inbound.ProductId)
            throw WarehouseException.Conflict($"Shelf {shelf.Code} already holds another product.", shelf.Id);
        if (shelf.OnHand + inbound.QtyCartons > shelf.MaxCapacity)
            throw WarehouseException.Conflict($"Shelf {shelf.Code} is full.", shelf.Id);

        await repo.BeginTransactionAsync(ct);
        try
        {
            var placed = await shelves.TryPlaceInboundAsync(shelf.Id, inbound.ProductId, inbound.QtyCartons, ct);
            if (!placed)
                throw WarehouseException.Conflict($"Shelf {shelf.Code} is full.", shelf.Id);

            inbound.Status = InboundStatuses.Placed;
            inbound.PlacedAt = DateTime.UtcNow;
            inbound.PlacedByWorkerId = workerId;
            await repo.SaveChangesAsync(ct);
            await repo.CommitTransactionAsync(ct);
            return (await repo.GetInboundAsync(inbound.Id, ct))!.ToDto();
        }
        catch
        {
            await repo.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
