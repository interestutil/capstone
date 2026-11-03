using SmartWarehouse.Contracts;

namespace SmartWarehouse.MockApi;

public sealed class WarehouseStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly List<UserRec> _users = [];
    private readonly List<BranchRec> _branches = [];
    private readonly List<ProductTypeRec> _types = [];
    private readonly List<ProductRec> _products = [];
    private readonly List<ShelfRec> _shelves = [];
    private readonly List<OrderRec> _orders = [];
    private readonly List<InboundRec> _inbounds = [];
    private readonly List<MapNodeRec> _nodes = [];
    private readonly List<MapEdgeRec> _edges = [];
    private int _nextOrderId = 1;
    private int _nextLineId = 1;
    private int _nextInboundId = 1;

    public WarehouseStore() => Seed();

    public StoreResult<LoginResponse> Login(LoginRequest request)
    {
        lock (_gate)
        {
            var user = _users.FirstOrDefault(u =>
                u.IsActive &&
                u.Email.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Password == request.Password);

            if (user is null)
                return StoreResult<LoginResponse>.Fail(401, new ApiError("Invalid email or password."));

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
            _sessions[token] = new Session(user.Id, user.Role);
            return StoreResult<LoginResponse>.Ok(new LoginResponse(token, user.Id, user.FullName, user.Role, user.Email));
        }
    }

    public Session? GetSession(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        lock (_gate) return _sessions.TryGetValue(token, out var session) ? session : null;
    }

    public IReadOnlyList<BranchDto> GetBranches()
    {
        lock (_gate) return _branches.Select(b => new BranchDto(b.Id, b.Name)).ToList();
    }

    public IReadOnlyList<ProductDto> GetProducts()
    {
        lock (_gate) return _products.Select(ToProduct).ToList();
    }

    public StoreResult<ProductDto> GetProductByBarcode(string code)
    {
        lock (_gate)
        {
            var product = FindProductByBarcode(code);
            return product is null
                ? StoreResult<ProductDto>.Fail(404, new ApiError("Unknown barcode."))
                : StoreResult<ProductDto>.Ok(ToProduct(product));
        }
    }

    public IReadOnlyList<ShelfDto> GetShelves()
    {
        lock (_gate) return _shelves.Select(ToShelf).ToList();
    }

    public IReadOnlyList<InventoryRowDto> GetInventory()
    {
        lock (_gate)
        {
            return _products.Select(p =>
            {
                var holding = _shelves.Where(s => s.ProductId == p.Id).ToList();
                var onHand = holding.Sum(s => s.OnHand);
                var committed = holding.Sum(s => s.Committed);
                return new InventoryRowDto(p.Id, p.Name, onHand, committed, onHand - committed, holding.Count);
            }).ToList();
        }
    }

    public IReadOnlyList<WorkerDto> GetWorkers()
    {
        lock (_gate)
        {
            return _users
                .Where(u => u.Role == Roles.Worker)
                .Select(u => new WorkerDto(u.Id, u.Email, u.FullName, u.IsActive))
                .ToList();
        }
    }

    public StoreResult<OrderDetailDto> CreateOrder(int adminId, CreateOrderRequest request)
    {
        lock (_gate)
        {
            if (request.Lines is null || request.Lines.Count == 0)
                return StoreResult<OrderDetailDto>.Fail(400, new ApiError("An order needs at least one line."));
            if (_branches.All(b => b.Id != request.BranchId))
                return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Unknown branch."));
            if (request.Lines.Any(l => l.AssignedWorkerId == 0))
                return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Every line must have a worker."));

            foreach (var line in request.Lines)
            {
                var shelf = _shelves.FirstOrDefault(s => s.Id == line.ShelfId);
                if (shelf is null)
                    return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown shelf."));
                if (shelf.ProductId != line.ProductId)
                    return StoreResult<OrderDetailDto>.Fail(400, new ApiError($"Shelf {shelf.Code} does not hold that product."));
                if (_products.All(p => p.Id != line.ProductId))
                    return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown product."));
                if (!_users.Any(u => u.Id == line.AssignedWorkerId && u.Role == Roles.Worker))
                    return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Unknown worker."));
                if (line.QtyCartons <= 0)
                    return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Quantity must be greater than zero."));

                var available = shelf.OnHand - shelf.Committed;
                if (available < line.QtyCartons)
                {
                    return StoreResult<OrderDetailDto>.Fail(409, new ApiError(
                        $"Insufficient available stock on shelf {shelf.Code}.",
                        shelf.Id, line.QtyCartons, available));
                }
            }

            var snapshot = _shelves.ToDictionary(s => s.Id, s => (s.OnHand, s.Committed));
            foreach (var line in request.Lines)
            {
                var shelf = _shelves.First(s => s.Id == line.ShelfId);
                if (shelf.OnHand - shelf.Committed < line.QtyCartons)
                {
                    foreach (var pair in snapshot)
                    {
                        var restore = _shelves.First(s => s.Id == pair.Key);
                        restore.OnHand = pair.Value.OnHand;
                        restore.Committed = pair.Value.Committed;
                    }
                    return StoreResult<OrderDetailDto>.Fail(409, new ApiError(
                        $"Insufficient available stock on shelf {shelf.Code}.",
                        shelf.Id, line.QtyCartons, shelf.OnHand - shelf.Committed));
                }
                shelf.Committed += line.QtyCartons;
            }

            var order = new OrderRec
            {
                Id = _nextOrderId++,
                BranchId = request.BranchId,
                Deadline = request.Deadline,
                Status = OrderStatuses.Assigned,
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            var sorted = request.Lines
                .Select(l => (Line: l, Shelf: _shelves.First(s => s.Id == l.ShelfId)))
                .OrderBy(x => x.Shelf.Rack)
                .ThenBy(x => x.Shelf.Level)
                .ToList();

            var sequence = 1;
            foreach (var item in sorted)
            {
                order.Lines.Add(new LineRec
                {
                    Id = _nextLineId++,
                    OrderId = order.Id,
                    ShelfId = item.Line.ShelfId,
                    ProductId = item.Line.ProductId,
                    QtyCartons = item.Line.QtyCartons,
                    Status = LineStatuses.Pending,
                    PickSequence = sequence++,
                    AssignedWorkerId = item.Line.AssignedWorkerId
                });
            }

            _orders.Add(order);
            return StoreResult<OrderDetailDto>.Created(ToOrder(order, includeAllLines: true));
        }
    }

    public IReadOnlyList<OrderSummaryDto> GetOrders(string? status)
    {
        lock (_gate)
        {
            var query = _orders.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

            return query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryDto(
                    o.Id,
                    BranchName(o.BranchId),
                    o.Deadline,
                    o.Status,
                    o.CreatedAt,
                    o.Lines.Count))
                .ToList();
        }
    }

    public StoreResult<OrderDetailDto> GetOrder(int id, Session session)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order."));

            if (session.Role == Roles.Worker && order.Lines.All(l => l.AssignedWorkerId != session.UserId))
                return StoreResult<OrderDetailDto>.Fail(403, new ApiError("You are not assigned to this order."));

            var includeAll = session.Role == Roles.Admin;
            return StoreResult<OrderDetailDto>.Ok(ToOrder(order, includeAll, session.UserId));
        }
    }

    public StoreResult<OrderDetailDto> Cancel(int id)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order."));
            if (order.Status != OrderStatuses.Assigned)
                return StoreResult<OrderDetailDto>.Fail(409, new ApiError("Cancel is allowed only before the first confirmed pick."));

            foreach (var line in order.Lines.Where(l => l.Status == LineStatuses.Pending))
            {
                var shelf = _shelves.First(s => s.Id == line.ShelfId);
                shelf.Committed -= line.QtyCartons;
            }

            order.Status = OrderStatuses.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            return StoreResult<OrderDetailDto>.Ok(ToOrder(order, true));
        }
    }

    public StoreResult<OrderDetailDto> Reassign(int orderId, int lineId, int workerId)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order."));
            var line = order.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order line."));
            if (line.Status != LineStatuses.Pending)
                return StoreResult<OrderDetailDto>.Fail(409, new ApiError("A picked line cannot be reassigned."));
            if (_users.All(u => u.Id != workerId || u.Role != Roles.Worker))
                return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Unknown worker."));

            line.AssignedWorkerId = workerId;
            return StoreResult<OrderDetailDto>.Ok(ToOrder(order, true));
        }
    }

    public IReadOnlyList<OrderDetailDto> GetMyOrders(int workerId)
    {
        lock (_gate)
        {
            return _orders
                .Where(o => o.Status is not OrderStatuses.Cancelled and not OrderStatuses.Completed)
                .Where(o => o.Lines.Any(l => l.AssignedWorkerId == workerId))
                .OrderBy(o => o.Deadline)
                .Select(o => ToOrder(o, includeAllLines: false, workerId))
                .ToList();
        }
    }

    public StoreResult<OrderDetailDto> ConfirmPick(int orderId, int lineId, int workerId, string? barcode)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order."));
            var line = order.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order line."));
            if (line.AssignedWorkerId != workerId)
                return StoreResult<OrderDetailDto>.Fail(403, new ApiError("You are not assigned to this line."));
            if (line.Status != LineStatuses.Pending)
                return StoreResult<OrderDetailDto>.Fail(409, new ApiError("This line has already been picked."));

            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var product = _products.First(p => p.Id == line.ProductId);
                if (!product.PieceBarcode.Equals(barcode, StringComparison.OrdinalIgnoreCase) &&
                    !product.CartonBarcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    return StoreResult<OrderDetailDto>.Fail(400, new ApiError("Scanned barcode does not match this product."));
                }
            }

            var shelf = _shelves.First(s => s.Id == line.ShelfId);
            shelf.OnHand -= line.QtyCartons;
            shelf.Committed -= line.QtyCartons;
            if (shelf.OnHand == 0 && shelf.Committed == 0)
                shelf.ProductId = null;

            line.Status = LineStatuses.Picked;
            line.PickedAt = DateTime.UtcNow;

            if (order.Status == OrderStatuses.Assigned)
            {
                order.Status = OrderStatuses.InProgress;
                order.FirstPickAt = DateTime.UtcNow;
            }

            if (order.Lines.All(l => l.Status == LineStatuses.Picked))
                order.Status = OrderStatuses.AwaitingHandoff;

            return StoreResult<OrderDetailDto>.Ok(ToOrder(order, false, workerId));
        }
    }

    public StoreResult<OrderDetailDto> ConfirmHandoff(int orderId, int workerId)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null)
                return StoreResult<OrderDetailDto>.Fail(404, new ApiError("Unknown order."));

            var mine = order.Lines.Where(l => l.AssignedWorkerId == workerId).ToList();
            if (mine.Count == 0)
                return StoreResult<OrderDetailDto>.Fail(403, new ApiError("You are not assigned to this order."));
            if (mine.Any(l => l.Status != LineStatuses.Picked))
                return StoreResult<OrderDetailDto>.Fail(409, new ApiError("Handoff is allowed only after your last pick."));

            var now = DateTime.UtcNow;
            foreach (var line in mine.Where(l => l.HandedOffAt is null))
                line.HandedOffAt = now;

            if (order.Lines.All(l => l.HandedOffAt is not null))
            {
                order.Status = OrderStatuses.Completed;
                order.CompletedAt = now;
            }

            return StoreResult<OrderDetailDto>.Ok(ToOrder(order, false, workerId));
        }
    }

    public StoreResult<InboundDto> CreateInbound(int workerId, CreateInboundRequest request)
    {
        lock (_gate)
        {
            var product = FindProductByBarcode(request.Barcode);
            if (product is null)
                return StoreResult<InboundDto>.Fail(404, new ApiError("Unknown barcode."));
            if (request.QtyCartons <= 0)
                return StoreResult<InboundDto>.Fail(400, new ApiError("Quantity must be greater than zero."));
            if (string.IsNullOrWhiteSpace(request.VoucherNumber))
                return StoreResult<InboundDto>.Fail(400, new ApiError("Voucher number is required."));

            var inbound = new InboundRec
            {
                Id = _nextInboundId++,
                VoucherNumber = request.VoucherNumber.Trim(),
                ProductId = product.Id,
                QtyCartons = request.QtyCartons,
                Status = InboundStatuses.Received,
                CreatedByWorkerId = workerId,
                CreatedAt = DateTime.UtcNow
            };
            _inbounds.Add(inbound);
            return StoreResult<InboundDto>.Created(ToInbound(inbound));
        }
    }

    public IReadOnlyList<InboundDto> GetInbound()
    {
        lock (_gate) return _inbounds.OrderByDescending(i => i.CreatedAt).Select(ToInbound).ToList();
    }

    public StoreResult<InboundDto> AssignShelf(int inboundId, int shelfId)
    {
        lock (_gate)
        {
            var inbound = _inbounds.FirstOrDefault(i => i.Id == inboundId);
            if (inbound is null)
                return StoreResult<InboundDto>.Fail(404, new ApiError("Unknown inbound record."));
            if (inbound.Status == InboundStatuses.Placed)
                return StoreResult<InboundDto>.Fail(409, new ApiError("This inbound stock has already been placed."));

            var shelf = _shelves.FirstOrDefault(s => s.Id == shelfId);
            if (shelf is null)
                return StoreResult<InboundDto>.Fail(404, new ApiError("Unknown shelf."));

            var product = _products.First(p => p.Id == inbound.ProductId);
            if (shelf.OnHand >= shelf.MaxCapacity)
                return StoreResult<InboundDto>.Fail(409, new ApiError($"Shelf {shelf.Code} is full.", shelf.Id));
            if (shelf.ProductId is not null && shelf.ProductId != product.Id)
                return StoreResult<InboundDto>.Fail(409, new ApiError($"Shelf {shelf.Code} already holds another product.", shelf.Id));
            if (shelf.OnHand + inbound.QtyCartons > shelf.MaxCapacity)
                return StoreResult<InboundDto>.Fail(409, new ApiError($"Shelf {shelf.Code} does not have room for {inbound.QtyCartons} cartons.", shelf.Id, inbound.QtyCartons, shelf.MaxCapacity - shelf.OnHand));

            inbound.TargetShelfId = shelf.Id;
            inbound.Status = InboundStatuses.ShelfAssigned;
            return StoreResult<InboundDto>.Ok(ToInbound(inbound));
        }
    }

    public StoreResult<InboundDto> PlaceInbound(int inboundId, int workerId)
    {
        lock (_gate)
        {
            var inbound = _inbounds.FirstOrDefault(i => i.Id == inboundId);
            if (inbound is null)
                return StoreResult<InboundDto>.Fail(404, new ApiError("Unknown inbound record."));
            if (inbound.Status != InboundStatuses.ShelfAssigned || inbound.TargetShelfId is null)
                return StoreResult<InboundDto>.Fail(409, new ApiError("The admin has not assigned a shelf yet."));

            var shelf = _shelves.First(s => s.Id == inbound.TargetShelfId);
            if (shelf.ProductId is not null && shelf.ProductId != inbound.ProductId)
                return StoreResult<InboundDto>.Fail(409, new ApiError($"Shelf {shelf.Code} already holds another product.", shelf.Id));
            if (shelf.OnHand + inbound.QtyCartons > shelf.MaxCapacity)
                return StoreResult<InboundDto>.Fail(409, new ApiError($"Shelf {shelf.Code} is full.", shelf.Id));

            shelf.ProductId = inbound.ProductId;
            shelf.OnHand += inbound.QtyCartons;
            inbound.Status = InboundStatuses.Placed;
            inbound.PlacedAt = DateTime.UtcNow;
            inbound.PlacedByWorkerId = workerId;
            return StoreResult<InboundDto>.Ok(ToInbound(inbound));
        }
    }

    public MapInfoDto GetMap(string imageUrl) => new(imageUrl, 800, 500);

    public StoreResult<RouteDto> GetRoute(int orderId, int workerId, int anchorShelfId)
    {
        lock (_gate)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order is null)
                return StoreResult<RouteDto>.Fail(404, new ApiError("Unknown order."));

            var remaining = order.Lines
                .Where(l => l.AssignedWorkerId == workerId && l.Status == LineStatuses.Pending)
                .OrderBy(l => l.PickSequence)
                .ToList();

            if (order.Lines.All(l => l.AssignedWorkerId != workerId))
                return StoreResult<RouteDto>.Fail(403, new ApiError("You are not assigned to this order."));

            var anchor = _shelves.FirstOrDefault(s => s.Id == anchorShelfId);
            if (anchor is null)
                return StoreResult<RouteDto>.Fail(404, new ApiError("Unknown shelf."));

            var current = anchor.NodeId;
            var legs = new List<RouteLegDto>();
            var sequence = 1;

            foreach (var line in remaining)
            {
                var shelf = _shelves.First(s => s.Id == line.ShelfId);
                var path = ShortestPath(current, shelf.NodeId);
                if (path is null)
                    return StoreResult<RouteDto>.Fail(422, new ApiError("Route cannot be computed because a node is unreachable.", NodeId: shelf.NodeId));

                legs.Add(new RouteLegDto(sequence++, "shelf", shelf.Id, line.Id, ToPoints(path), PathDistance(path)));
                current = shelf.NodeId;
            }

            var dock = _nodes.First(n => n.Type == "Dock");
            var toDock = ShortestPath(current, dock.Id);
            if (toDock is null)
                return StoreResult<RouteDto>.Fail(422, new ApiError("Route cannot be computed because a node is unreachable.", NodeId: dock.Id));

            legs.Add(new RouteLegDto(sequence, "dock", null, null, ToPoints(toDock), PathDistance(toDock)));
            return StoreResult<RouteDto>.Ok(new RouteDto(order.Id, legs));
        }
    }

    public IReadOnlyList<LedDto> GetLeds(string deviceId)
    {
        lock (_gate)
        {
            var nextByWorker = _orders
                .Where(o => o.Status is OrderStatuses.Assigned or OrderStatuses.InProgress)
                .SelectMany(o => o.Lines)
                .Where(l => l.Status == LineStatuses.Pending)
                .GroupBy(l => l.AssignedWorkerId)
                .Select(g => g.OrderBy(l => l.PickSequence).First())
                .Select(l => l.ShelfId)
                .ToHashSet();

            return _shelves
                .Where(s => string.Equals(s.LedDeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.LedIndex)
                .Select(s => new LedDto(s.LedIndex, nextByWorker.Contains(s.Id), s.Code))
                .ToList();
        }
    }

    private ProductRec? FindProductByBarcode(string code) =>
        _products.FirstOrDefault(p =>
            p.PieceBarcode.Equals(code, StringComparison.OrdinalIgnoreCase) ||
            p.CartonBarcode.Equals(code, StringComparison.OrdinalIgnoreCase));

    private ProductDto ToProduct(ProductRec p)
    {
        var type = _types.First(t => t.Id == p.ProductTypeId);
        return new ProductDto(p.Id, p.Name, p.ProductTypeId, type.Name, p.PieceBarcode, p.CartonBarcode);
    }

    private ShelfDto ToShelf(ShelfRec s)
    {
        var type = _types.First(t => t.Id == s.ProductTypeId);
        var product = _products.FirstOrDefault(p => p.Id == s.ProductId);
        var available = s.OnHand - s.Committed;
        return new ShelfDto(
            s.Id, s.Code, s.Rack, s.Level, s.ProductTypeId, type.Name,
            s.ProductId, product?.Name, s.MaxCapacity, s.OnHand, s.Committed, available,
            s.OnHand == 0 && s.Committed == 0, s.OnHand >= s.MaxCapacity,
            s.LedDeviceId, s.LedIndex);
    }

    private OrderDetailDto ToOrder(OrderRec order, bool includeAllLines, int? workerId = null)
    {
        var lines = includeAllLines
            ? order.Lines
            : order.Lines.Where(l => l.AssignedWorkerId == workerId).ToList();

        return new OrderDetailDto(
            order.Id, order.BranchId, BranchName(order.BranchId), order.Deadline, order.Status,
            order.CreatedAt, order.FirstPickAt, order.CompletedAt, order.CancelledAt,
            lines.OrderBy(l => l.PickSequence).Select(ToLine).ToList());
    }

    private OrderLineDto ToLine(LineRec line)
    {
        var shelf = _shelves.First(s => s.Id == line.ShelfId);
        var product = _products.First(p => p.Id == line.ProductId);
        var worker = _users.First(u => u.Id == line.AssignedWorkerId);
        return new OrderLineDto(
            line.Id, shelf.Id, shelf.Code, product.Id, product.Name, line.QtyCartons,
            line.Status, line.PickSequence, worker.Id, worker.FullName, line.PickedAt, line.HandedOffAt);
    }

    private InboundDto ToInbound(InboundRec inbound)
    {
        var product = _products.First(p => p.Id == inbound.ProductId);
        var worker = _users.First(u => u.Id == inbound.CreatedByWorkerId);
        var shelf = _shelves.FirstOrDefault(s => s.Id == inbound.TargetShelfId);
        return new InboundDto(
            inbound.Id, inbound.VoucherNumber, product.Id, product.Name, inbound.QtyCartons,
            inbound.TargetShelfId, shelf?.Code, inbound.Status, worker.Id, worker.FullName,
            inbound.CreatedAt, inbound.PlacedAt);
    }

    private string BranchName(int id) => _branches.First(b => b.Id == id).Name;

    private List<PointDto> ToPoints(List<int> nodeIds) =>
        nodeIds.Select(id =>
        {
            var node = _nodes.First(n => n.Id == id);
            return new PointDto(node.X, node.Y);
        }).ToList();

    private double PathDistance(List<int> nodeIds)
    {
        double total = 0;
        for (var i = 0; i < nodeIds.Count - 1; i++)
        {
            var a = _nodes.First(n => n.Id == nodeIds[i]);
            var b = _nodes.First(n => n.Id == nodeIds[i + 1]);
            total += Distance(a, b);
        }
        return Math.Round(total, 1);
    }

    private static double Distance(MapNodeRec a, MapNodeRec b) =>
        Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private List<int>? ShortestPath(int fromId, int toId)
    {
        if (fromId == toId) return [fromId];

        var nodes = _nodes.Select(n => n.Id).ToList();
        var dist = nodes.ToDictionary(id => id, _ => double.PositiveInfinity);
        var prev = new Dictionary<int, int>();
        var remaining = nodes.ToHashSet();
        dist[fromId] = 0;

        while (remaining.Count > 0)
        {
            var current = remaining.MinBy(id => dist[id]);
            if (double.IsPositiveInfinity(dist[current]))
                return null;
            remaining.Remove(current);
            if (current == toId) break;

            foreach (var edge in _edges.Where(e => e.FromId == current || e.ToId == current))
            {
                var next = edge.FromId == current ? edge.ToId : edge.FromId;
                if (!remaining.Contains(next)) continue;
                var alt = dist[current] + Distance(_nodes.First(n => n.Id == current), _nodes.First(n => n.Id == next));
                if (alt < dist[next])
                {
                    dist[next] = alt;
                    prev[next] = current;
                }
            }
        }

        if (!prev.ContainsKey(toId) && fromId != toId)
            return fromId == toId ? [fromId] : null;

        var path = new List<int> { toId };
        var node = toId;
        while (prev.TryGetValue(node, out var parent))
        {
            path.Add(parent);
            node = parent;
        }
        path.Reverse();
        return path[0] == fromId ? path : null;
    }

    private void Seed()
    {
        _users.AddRange([
            new UserRec(1, "admin@warehouse.local", "Admin123!", "Nour Hassan", Roles.Admin, true),
            new UserRec(2, "ahmed@warehouse.local", "Worker123!", "Ahmed Farouk", Roles.Worker, true),
            new UserRec(3, "sara@warehouse.local", "Worker123!", "Sara Adel", Roles.Worker, true)
        ]);

        _branches.AddRange([
            new BranchRec(1, "Smouha"),
            new BranchRec(2, "El Asafra")
        ]);

        _types.AddRange([
            new ProductTypeRec(1, "Cleaning supplies"),
            new ProductTypeRec(2, "Canned food")
        ]);

        _products.AddRange([
            new ProductRec(1, "Laundry detergent", 1, "P-1001", "C-1001"),
            new ProductRec(2, "Floor bleach", 1, "P-1002", "C-1002"),
            new ProductRec(3, "Canned beans", 2, "P-2001", "C-2001"),
            new ProductRec(4, "Canned tomatoes", 2, "P-2002", "C-2002")
        ]);

        _nodes.AddRange([
            new MapNodeRec(1, 80, 440, "Dock"),
            new MapNodeRec(2, 80, 200, "Aisle"),
            new MapNodeRec(3, 400, 200, "Aisle"),
            new MapNodeRec(4, 720, 200, "Aisle"),
            new MapNodeRec(5, 200, 120, "ShelfAnchor"),
            new MapNodeRec(6, 320, 120, "ShelfAnchor"),
            new MapNodeRec(7, 480, 120, "ShelfAnchor"),
            new MapNodeRec(8, 600, 120, "ShelfAnchor"),
            new MapNodeRec(9, 720, 120, "ShelfAnchor"),
            new MapNodeRec(10, 200, 280, "ShelfAnchor")
        ]);

        _edges.AddRange([
            new MapEdgeRec(1, 2),
            new MapEdgeRec(2, 3),
            new MapEdgeRec(3, 4),
            new MapEdgeRec(2, 5),
            new MapEdgeRec(3, 6),
            new MapEdgeRec(3, 7),
            new MapEdgeRec(4, 8),
            new MapEdgeRec(4, 9),
            new MapEdgeRec(2, 10)
        ]);

        _shelves.AddRange([
            new ShelfRec { Id = 1, Code = "R1-L1", Rack = 1, Level = 1, ProductTypeId = 1, ProductId = 1, MaxCapacity = 24, OnHand = 20, NodeId = 5, LedDeviceId = "esp32-1", LedIndex = 0 },
            new ShelfRec { Id = 2, Code = "R1-L2", Rack = 1, Level = 2, ProductTypeId = 1, ProductId = null, MaxCapacity = 24, OnHand = 0, NodeId = 6, LedDeviceId = "esp32-1", LedIndex = 1 },
            new ShelfRec { Id = 3, Code = "R2-L1", Rack = 2, Level = 1, ProductTypeId = 2, ProductId = 3, MaxCapacity = 24, OnHand = 18, NodeId = 7, LedDeviceId = "esp32-1", LedIndex = 2 },
            new ShelfRec { Id = 4, Code = "R2-L2", Rack = 2, Level = 2, ProductTypeId = 2, ProductId = null, MaxCapacity = 24, OnHand = 0, NodeId = 8, LedDeviceId = "esp32-1", LedIndex = 3 },
            new ShelfRec { Id = 5, Code = "R2-L3", Rack = 2, Level = 3, ProductTypeId = 2, ProductId = 4, MaxCapacity = 24, OnHand = 10, NodeId = 9, LedDeviceId = "esp32-1", LedIndex = 4 },
            new ShelfRec { Id = 6, Code = "R3-L1", Rack = 3, Level = 1, ProductTypeId = 1, ProductId = 2, MaxCapacity = 16, OnHand = 8, NodeId = 10, LedDeviceId = "esp32-1", LedIndex = 5 }
        ]);
    }

    public sealed record Session(int UserId, string Role);

    private sealed record UserRec(int Id, string Email, string Password, string FullName, string Role, bool IsActive);
    private sealed record BranchRec(int Id, string Name);
    private sealed record ProductTypeRec(int Id, string Name);
    private sealed record ProductRec(int Id, string Name, int ProductTypeId, string PieceBarcode, string CartonBarcode);
    private sealed record MapNodeRec(int Id, double X, double Y, string Type);
    private sealed record MapEdgeRec(int FromId, int ToId);

    private sealed class ShelfRec
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public int Rack { get; set; }
        public int Level { get; set; }
        public int ProductTypeId { get; set; }
        public int? ProductId { get; set; }
        public int MaxCapacity { get; set; }
        public int OnHand { get; set; }
        public int Committed { get; set; }
        public int NodeId { get; set; }
        public string? LedDeviceId { get; set; }
        public int LedIndex { get; set; }
    }

    private sealed class OrderRec
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "";
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FirstPickAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public List<LineRec> Lines { get; } = [];
    }

    private sealed class LineRec
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ShelfId { get; set; }
        public int ProductId { get; set; }
        public int QtyCartons { get; set; }
        public string Status { get; set; } = "";
        public int PickSequence { get; set; }
        public int AssignedWorkerId { get; set; }
        public DateTime? PickedAt { get; set; }
        public DateTime? HandedOffAt { get; set; }
    }

    private sealed class InboundRec
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = "";
        public int ProductId { get; set; }
        public int QtyCartons { get; set; }
        public int? TargetShelfId { get; set; }
        public string Status { get; set; } = "";
        public int CreatedByWorkerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PlacedAt { get; set; }
        public int? PlacedByWorkerId { get; set; }
    }
}

public readonly record struct StoreResult<T>(int Status, T? Value, ApiError? Error)
{
    public bool IsSuccess => Status is >= 200 and < 300;
    public static StoreResult<T> Ok(T value) => new(200, value, null);
    public static StoreResult<T> Created(T value) => new(201, value, null);
    public static StoreResult<T> Fail(int status, ApiError error) => new(status, default, error);
}
