using SmartWarehouse.Api.Errors;
using SmartWarehouse.Api.Mapping;
using SmartWarehouse.Contracts;
using SmartWarehouse.Domain;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Repositories;

namespace SmartWarehouse.Api.Services;

public sealed class OrderService(IWarehouseRepository repo, IShelfRepository shelves)
{
    public async Task<OrderDetailDto> CreateAsync(int adminId, CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw WarehouseException.BadRequest("An order needs at least one line.");

        var branches = await repo.ListBranchesAsync(ct);
        if (branches.All(b => b.Id != request.BranchId))
            throw WarehouseException.BadRequest("Unknown branch.");
        if (request.Lines.Any(l => l.AssignedWorkerId == 0))
            throw WarehouseException.BadRequest("Every line must have a worker.");

        var workers = await repo.ListWorkersAsync(ct);
        var requestedByShelf = new Dictionary<int, int>();

        foreach (var line in request.Lines)
        {
            if (line.QtyCartons <= 0)
                throw WarehouseException.BadRequest("Quantity must be greater than zero.");

            var shelf = await shelves.GetByIdAsync(line.ShelfId, ct)
                        ?? throw WarehouseException.NotFound("Unknown shelf.");
            var product = await repo.GetProductAsync(line.ProductId, ct)
                          ?? throw WarehouseException.NotFound("Unknown product.");
            if (shelf.ProductId != line.ProductId)
                throw WarehouseException.BadRequest($"Shelf {shelf.Code} does not hold that product.");
            if (workers.All(u => u.Id != line.AssignedWorkerId))
                throw WarehouseException.BadRequest("Unknown worker.");

            requestedByShelf.TryGetValue(shelf.Id, out var already);
            var available = shelf.Available - already;
            if (available < line.QtyCartons)
            {
                throw WarehouseException.Conflict(
                    $"Insufficient available stock on shelf {shelf.Code}.",
                    shelf.Id, line.QtyCartons, available);
            }

            requestedByShelf[shelf.Id] = already + line.QtyCartons;
        }

        await repo.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in request.Lines)
            {
                var committed = await shelves.TryIncreaseCommittedAsync(line.ShelfId, line.QtyCartons, ct);
                if (!committed)
                {
                    var shelf = await shelves.GetByIdAsync(line.ShelfId, ct);
                    await repo.RollbackTransactionAsync(ct);
                    throw WarehouseException.Conflict(
                        $"Insufficient available stock on shelf {shelf?.Code ?? line.ShelfId.ToString()}.",
                        line.ShelfId, line.QtyCartons, shelf?.Available ?? 0);
                }
            }

            var shelfRows = await shelves.ListAsync(ct);
            var sorted = request.Lines
                .Select(l => (Line: l, Shelf: shelfRows.First(s => s.Id == l.ShelfId)))
                .OrderBy(x => x.Shelf.Rack)
                .ThenBy(x => x.Shelf.Level)
                .ToList();

            var order = new Order
            {
                BranchId = request.BranchId,
                Deadline = request.Deadline,
                Status = OrderStatuses.Assigned,
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            var sequence = 1;
            foreach (var item in sorted)
            {
                order.Lines.Add(new OrderLine
                {
                    ShelfId = item.Line.ShelfId,
                    ProductId = item.Line.ProductId,
                    QtyCartons = item.Line.QtyCartons,
                    Status = LineStatuses.Pending,
                    PickSequence = sequence++,
                    AssignedWorkerId = item.Line.AssignedWorkerId
                });
            }

            repo.AddOrder(order);
            await repo.SaveChangesAsync(ct);
            await repo.CommitTransactionAsync(ct);

            var created = await repo.GetOrderAsync(order.Id, ct)
                          ?? throw new InvalidOperationException("Order was not saved.");
            return created.ToDetail(true);
        }
        catch
        {
            await repo.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<List<OrderSummaryDto>> ListAsync(string? status, CancellationToken ct) =>
        (await repo.ListOrdersAsync(status, ct)).Select(o => o.ToSummary()).ToList();

    public async Task<OrderDetailDto> GetAsync(int id, int userId, string role, CancellationToken ct)
    {
        var order = await RequireOrder(id, ct);
        if (role == UserRoles.Worker && order.Lines.All(l => l.AssignedWorkerId != userId))
            throw WarehouseException.Forbidden("You are not assigned to this order.");
        return order.ToDetail(role == UserRoles.Admin, userId);
    }

    public async Task<List<OrderDetailDto>> GetMineAsync(int workerId, CancellationToken ct)
    {
        var orders = await repo.ListOrdersAsync(null, ct);
        var ids = orders
            .Where(o => o.Status is not OrderStatuses.Cancelled and not OrderStatuses.Completed)
            .Where(o => o.Lines.Any(l => l.AssignedWorkerId == workerId))
            .OrderBy(o => o.Deadline)
            .Select(o => o.Id)
            .ToList();

        var details = new List<OrderDetailDto>();
        foreach (var id in ids)
        {
            var order = await RequireOrder(id, ct);
            details.Add(order.ToDetail(false, workerId));
        }

        return details;
    }

    public async Task<OrderDetailDto> CancelAsync(int id, CancellationToken ct)
    {
        var order = await RequireOrder(id, ct);
        if (order.Status != OrderStatuses.Assigned)
            throw WarehouseException.Conflict("Cancel is allowed only before the first confirmed pick.");

        await repo.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in order.Lines.Where(l => l.Status == LineStatuses.Pending))
            {
                var released = await shelves.TryDecreaseCommittedAsync(line.ShelfId, line.QtyCartons, ct);
                if (!released)
                    throw WarehouseException.Conflict("Could not release committed stock.");
            }

            order.Status = OrderStatuses.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            await repo.SaveChangesAsync(ct);
            await repo.CommitTransactionAsync(ct);
            return (await RequireOrder(id, ct)).ToDetail(true);
        }
        catch
        {
            await repo.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<OrderDetailDto> ReassignAsync(int orderId, int lineId, int workerId, CancellationToken ct)
    {
        var order = await RequireOrder(orderId, ct);
        var line = order.Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw WarehouseException.NotFound("Unknown order line.");
        if (line.Status != LineStatuses.Pending)
            throw WarehouseException.Conflict("A picked line cannot be reassigned.");

        var worker = await repo.GetUserAsync(workerId, ct);
        if (worker is null || worker.Role != UserRoles.Worker)
            throw WarehouseException.BadRequest("Unknown worker.");

        line.AssignedWorkerId = workerId;
        await repo.SaveChangesAsync(ct);
        return (await RequireOrder(orderId, ct)).ToDetail(true);
    }

    public async Task<OrderDetailDto> ConfirmPickAsync(int orderId, int lineId, int workerId, string? barcode, CancellationToken ct)
    {
        var order = await RequireOrder(orderId, ct);
        var line = order.Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw WarehouseException.NotFound("Unknown order line.");
        if (line.AssignedWorkerId != workerId)
            throw WarehouseException.Forbidden("You are not assigned to this line.");
        if (line.Status != LineStatuses.Pending)
            throw WarehouseException.Conflict("This line has already been picked.");

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var product = line.Product;
            if (!product.PieceBarcode.Equals(barcode, StringComparison.OrdinalIgnoreCase) &&
                !product.CartonBarcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
            {
                throw WarehouseException.BadRequest("Scanned barcode does not match this product.");
            }
        }

        await repo.BeginTransactionAsync(ct);
        try
        {
            var picked = await shelves.TryConfirmPickAsync(line.ShelfId, line.QtyCartons, ct);
            if (!picked)
                throw WarehouseException.Conflict("Could not deduct stock for this pick.");

            line.Status = LineStatuses.Picked;
            line.PickedAt = DateTime.UtcNow;
            if (order.Status == OrderStatuses.Assigned)
            {
                order.Status = OrderStatuses.InProgress;
                order.FirstPickAt = DateTime.UtcNow;
            }

            if (order.Lines.All(l => l.Status == LineStatuses.Picked))
            {
                order.Status = order.Lines.All(l => l.HandedOffAt is not null)
                    ? OrderStatuses.Completed
                    : OrderStatuses.AwaitingHandoff;
                if (order.Status == OrderStatuses.Completed)
                    order.CompletedAt = DateTime.UtcNow;
            }

            await repo.SaveChangesAsync(ct);
            await repo.CommitTransactionAsync(ct);
            return (await RequireOrder(orderId, ct)).ToDetail(false, workerId);
        }
        catch
        {
            await repo.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<OrderDetailDto> ConfirmHandoffAsync(int orderId, int workerId, CancellationToken ct)
    {
        var order = await RequireOrder(orderId, ct);
        var mine = order.Lines.Where(l => l.AssignedWorkerId == workerId).ToList();
        if (mine.Count == 0)
            throw WarehouseException.Forbidden("You are not assigned to this order.");
        if (mine.Any(l => l.Status != LineStatuses.Picked))
            throw WarehouseException.Conflict("Handoff is allowed only after your last pick.");

        var now = DateTime.UtcNow;
        foreach (var line in mine.Where(l => l.HandedOffAt is null))
            line.HandedOffAt = now;

        if (order.Lines.All(l => l.HandedOffAt is not null))
        {
            order.Status = OrderStatuses.Completed;
            order.CompletedAt = now;
        }

        await repo.SaveChangesAsync(ct);
        return (await RequireOrder(orderId, ct)).ToDetail(false, workerId);
    }

    private async Task<Order> RequireOrder(int id, CancellationToken ct) =>
        await repo.GetOrderAsync(id, ct) ?? throw WarehouseException.NotFound("Unknown order.");
}
