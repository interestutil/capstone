using SmartWarehouse.Api.Errors;
using SmartWarehouse.Contracts;
using SmartWarehouse.Domain;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Repositories;

namespace SmartWarehouse.Api.Services;

public sealed class RoutingService(IWarehouseRepository repo, IShelfRepository shelves)
{
    public async Task<MapInfoDto> GetMapAsync(string imageUrl, CancellationToken ct)
    {
        _ = ct;
        return new MapInfoDto(imageUrl, 800, 500);
    }

    public async Task<RouteDto> GetRouteAsync(int orderId, int workerId, int anchorShelfId, CancellationToken ct)
    {
        var order = await repo.GetOrderAsync(orderId, ct)
                    ?? throw WarehouseException.NotFound("Unknown order.");
        if (order.Lines.All(l => l.AssignedWorkerId != workerId))
            throw WarehouseException.Forbidden("You are not assigned to this order.");

        var remaining = order.Lines
            .Where(l => l.AssignedWorkerId == workerId && l.Status == LineStatuses.Pending)
            .OrderBy(l => l.PickSequence)
            .ToList();

        var anchor = await shelves.GetByIdAsync(anchorShelfId, ct)
                     ?? throw WarehouseException.NotFound("Unknown shelf.");

        var nodes = await repo.ListNodesAsync(ct);
        var edges = await repo.ListEdgesAsync(ct);
        var current = anchor.NodeId;
        var legs = new List<RouteLegDto>();
        var sequence = 1;

        foreach (var line in remaining)
        {
            var path = ShortestPath(nodes, edges, current, line.Shelf.NodeId);
            if (path is null)
                throw WarehouseException.Unreachable("Route cannot be computed because a node is unreachable.", line.Shelf.NodeId);

            legs.Add(new RouteLegDto(sequence++, "shelf", line.ShelfId, line.Id, ToPoints(nodes, path), PathDistance(nodes, path)));
            current = line.Shelf.NodeId;
        }

        var dock = nodes.FirstOrDefault(n => n.Type == MapNodeTypes.Dock)
                   ?? throw WarehouseException.Unreachable("Route cannot be computed because a node is unreachable.");
        var toDock = ShortestPath(nodes, edges, current, dock.Id);
        if (toDock is null)
            throw WarehouseException.Unreachable("Route cannot be computed because a node is unreachable.", dock.Id);

        legs.Add(new RouteLegDto(sequence, "dock", null, null, ToPoints(nodes, toDock), PathDistance(nodes, toDock)));
        return new RouteDto(order.Id, legs);
    }

    public async Task<List<LedDto>> GetLedsAsync(string deviceId, CancellationToken ct)
    {
        var orders = await repo.ListOrdersAsync(null, ct);
        var nextByWorker = new HashSet<int>();
        foreach (var group in orders
                     .Where(o => o.Status is OrderStatuses.Assigned or OrderStatuses.InProgress)
                     .SelectMany(o => o.Lines)
                     .Where(l => l.Status == LineStatuses.Pending)
                     .GroupBy(l => l.AssignedWorkerId))
        {
            nextByWorker.Add(group.OrderBy(l => l.PickSequence).First().ShelfId);
        }

        var shelfList = await shelves.ListAsync(ct);
        return shelfList
            .Where(s => string.Equals(s.LedDeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.LedIndex)
            .Select(s => new LedDto(s.LedIndex, nextByWorker.Contains(s.Id), s.Code))
            .ToList();
    }

    private static List<PointDto> ToPoints(IReadOnlyList<MapNode> nodes, List<int> path) =>
        path.Select(id =>
        {
            var node = nodes.First(n => n.Id == id);
            return new PointDto(node.X, node.Y);
        }).ToList();

    private static double PathDistance(IReadOnlyList<MapNode> nodes, List<int> path)
    {
        double total = 0;
        for (var i = 0; i < path.Count - 1; i++)
            total += Distance(nodes.First(n => n.Id == path[i]), nodes.First(n => n.Id == path[i + 1]));
        return Math.Round(total, 1);
    }

    private static double Distance(MapNode a, MapNode b) =>
        Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static List<int>? ShortestPath(IReadOnlyList<MapNode> nodes, IReadOnlyList<MapEdge> edges, int fromId, int toId)
    {
        if (fromId == toId)
            return [fromId];

        var ids = nodes.Select(n => n.Id).ToList();
        var dist = ids.ToDictionary(id => id, _ => double.PositiveInfinity);
        var prev = new Dictionary<int, int>();
        var remaining = ids.ToHashSet();
        dist[fromId] = 0;

        while (remaining.Count > 0)
        {
            var current = remaining.MinBy(id => dist[id]);
            if (double.IsPositiveInfinity(dist[current]))
                return null;
            remaining.Remove(current);
            if (current == toId)
                break;

            foreach (var edge in edges.Where(e => e.FromNodeId == current || e.ToNodeId == current))
            {
                var next = edge.FromNodeId == current ? edge.ToNodeId : edge.FromNodeId;
                if (!remaining.Contains(next))
                    continue;
                var alt = dist[current] + Distance(nodes.First(n => n.Id == current), nodes.First(n => n.Id == next));
                if (alt < dist[next])
                {
                    dist[next] = alt;
                    prev[next] = current;
                }
            }
        }

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
}
