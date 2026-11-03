using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Tests;

public sealed class WarehouseAcceptanceTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _client;

    public WarehouseAcceptanceTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Stock_pick_cancel_route_and_inbound_rules()
    {
        var admin = await Login("admin@warehouse.local", "Admin123!");
        var ahmed = await Login("ahmed@warehouse.local", "Worker123!");
        var sara = await Login("sara@warehouse.local", "Worker123!");

        UseToken(admin.Token);
        var shelves = await Get<List<ShelfDto>>("api/shelves");
        var r1 = shelves.Single(s => s.Code == "R1-L1");
        var r2 = shelves.Single(s => s.Code == "R2-L1");
        var r3 = shelves.Single(s => s.Code == "R3-L1");
        var empty = shelves.Single(s => s.Code == "R1-L2");
        Assert.True(empty.IsEmpty);

        var workers = await Get<List<WorkerDto>>("api/workers");
        var ahmedId = workers.Single(w => w.Email == "ahmed@warehouse.local").Id;
        var saraId = workers.Single(w => w.Email == "sara@warehouse.local").Id;

        var first = await Post<OrderDetailDto>("api/orders", new CreateOrderRequest(
            1, DateTime.UtcNow.AddHours(4),
            [new CreateOrderLineRequest(r1.Id, r1.ProductId!.Value, r1.Available - 1, ahmedId)]));
        Assert.Equal(OrderStatuses.Assigned, first.Status);

        var over = await PostRaw("api/orders", new CreateOrderRequest(
            1, DateTime.UtcNow.AddHours(4),
            [new CreateOrderLineRequest(r1.Id, r1.ProductId!.Value, 3, saraId)]));
        Assert.Equal(HttpStatusCode.Conflict, over.StatusCode);
        var overError = await over.Content.ReadFromJsonAsync<ApiError>(Json);
        Assert.Equal(r1.Id, overError!.ShelfId);

        UseToken(admin.Token);
        shelves = await Get<List<ShelfDto>>("api/shelves");
        Assert.Equal(1, shelves.Single(s => s.Id == r1.Id).Available);

        var shortOrder = await PostRaw("api/orders", new CreateOrderRequest(
            2, DateTime.UtcNow.AddHours(4),
            [
                new CreateOrderLineRequest(r2.Id, r2.ProductId!.Value, 1, ahmedId),
                new CreateOrderLineRequest(r3.Id, r3.ProductId!.Value, 1, ahmedId),
                new CreateOrderLineRequest(r1.Id, r1.ProductId!.Value, 5, saraId)
            ]));
        Assert.Equal(HttpStatusCode.Conflict, shortOrder.StatusCode);
        shelves = await Get<List<ShelfDto>>("api/shelves");
        Assert.Equal(r2.Committed, shelves.Single(s => s.Id == r2.Id).Committed);
        Assert.Equal(r3.Committed, shelves.Single(s => s.Id == r3.Id).Committed);

        var pickOrder = await Post<OrderDetailDto>("api/orders", new CreateOrderRequest(
            1, DateTime.UtcNow.AddHours(4),
            [
                new CreateOrderLineRequest(r2.Id, r2.ProductId!.Value, 2, ahmedId),
                new CreateOrderLineRequest(r3.Id, r3.ProductId!.Value, 2, saraId)
            ]));

        shelves = await Get<List<ShelfDto>>("api/shelves");
        var r2Before = shelves.Single(s => s.Id == r2.Id);
        var line = pickOrder.Lines.Single(l => l.ShelfId == r2.Id);

        UseToken(sara.Token);
        var forbidden = await PostRaw($"api/orders/{pickOrder.Id}/lines/{line.Id}/pick", new ConfirmPickRequest("P-2001"));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        UseToken(ahmed.Token);
        var picked = await Post<OrderDetailDto>($"api/orders/{pickOrder.Id}/lines/{line.Id}/pick", new ConfirmPickRequest("P-2001"));
        Assert.Equal(LineStatuses.Picked, picked.Lines.Single(l => l.Id == line.Id).Status);

        UseToken(admin.Token);
        shelves = await Get<List<ShelfDto>>("api/shelves");
        var r2After = shelves.Single(s => s.Id == r2.Id);
        Assert.Equal(r2Before.OnHand - 2, r2After.OnHand);
        Assert.Equal(r2Before.Committed - 2, r2After.Committed);
        Assert.Equal(r2Before.Available, r2After.Available);

        var cancelPicked = await PostRaw($"api/orders/{pickOrder.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.Conflict, cancelPicked.StatusCode);

        var cancellable = await Post<OrderDetailDto>("api/orders", new CreateOrderRequest(
            1, DateTime.UtcNow.AddHours(2),
            [new CreateOrderLineRequest(r3.Id, r3.ProductId!.Value, 1, saraId)]));
        shelves = await Get<List<ShelfDto>>("api/shelves");
        var committedBeforeCancel = shelves.Single(s => s.Id == r3.Id).Committed;
        await Post<OrderDetailDto>($"api/orders/{cancellable.Id}/cancel", null);
        shelves = await Get<List<ShelfDto>>("api/shelves");
        Assert.Equal(committedBeforeCancel - 1, shelves.Single(s => s.Id == r3.Id).Committed);

        UseToken(ahmed.Token);
        var route = await Get<RouteDto>($"api/orders/{first.Id}/route?anchorShelfId={r3.Id}");
        Assert.Equal(first.Id, route.OrderId);
        Assert.Equal("shelf", route.Legs.First().Kind);
        Assert.Equal("dock", route.Legs.Last().Kind);
        Assert.Contains(route.Legs, l => l.ShelfId == r1.Id);

        UseToken(ahmed.Token);
        var inbound = await Post<InboundDto>("api/inbound", new CreateInboundRequest("C-1001", 4, "V-100"));
        Assert.Equal(InboundStatuses.Received, inbound.Status);

        UseToken(admin.Token);
        inbound = await Put<InboundDto>($"api/inbound/{inbound.Id}/shelf", new AssignShelfRequest(empty.Id));
        Assert.Equal(InboundStatuses.ShelfAssigned, inbound.Status);

        UseToken(ahmed.Token);
        inbound = await Post<InboundDto>($"api/inbound/{inbound.Id}/place", null);
        Assert.Equal(InboundStatuses.Placed, inbound.Status);

        UseToken(admin.Token);
        shelves = await Get<List<ShelfDto>>("api/shelves");
        var placedShelf = shelves.Single(s => s.Id == empty.Id);
        Assert.Equal(r1.ProductId, placedShelf.ProductId);
        Assert.Equal(4, placedShelf.OnHand);
        Assert.False(placedShelf.IsEmpty);
    }

    [Fact]
    public async Task Login_rejects_bad_password()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest("admin@warehouse.local", "nope"), Json);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<LoginResponse> Login(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(Json))!;
    }

    private void UseToken(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<T> Get<T>(string path)
    {
        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<T> Post<T>(string path, object? body)
    {
        var response = await PostRaw(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private Task<HttpResponseMessage> PostRaw(string path, object? body) =>
        body is null
            ? _client.PostAsync(path, null)
            : _client.PostAsJsonAsync(path, body, Json);

    private async Task<T> Put<T>(string path, object body)
    {
        var response = await _client.PutAsJsonAsync(path, body, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }
}
