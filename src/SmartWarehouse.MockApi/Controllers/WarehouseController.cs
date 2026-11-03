using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.MockApi.Controllers;

[ApiController]
[Route("api")]
public sealed class WarehouseController(WarehouseStore store) : ControllerBase
{
    [HttpPost("auth/login")]
    public IActionResult Login(LoginRequest request) => ToResult(store.Login(request));

    [HttpGet("branches")]
    [RequireRole(Roles.Admin, Roles.Worker)]
    public IActionResult Branches() => Ok(store.GetBranches());

    [HttpGet("products")]
    [RequireRole(Roles.Admin, Roles.Worker)]
    public IActionResult Products() => Ok(store.GetProducts());

    [HttpGet("products/by-barcode/{code}")]
    [RequireRole(Roles.Worker)]
    public IActionResult ProductByBarcode(string code) => ToResult(store.GetProductByBarcode(code));

    [HttpGet("shelves")]
    [RequireRole(Roles.Admin, Roles.Worker)]
    public IActionResult Shelves() => Ok(store.GetShelves());

    [HttpGet("inventory")]
    [RequireRole(Roles.Admin)]
    public IActionResult Inventory() => Ok(store.GetInventory());

    [HttpGet("workers")]
    [RequireRole(Roles.Admin)]
    public IActionResult Workers() => Ok(store.GetWorkers());

    [HttpPost("orders")]
    [RequireRole(Roles.Admin)]
    public IActionResult CreateOrder(CreateOrderRequest request) =>
        ToResult(store.CreateOrder(HttpContext.Session().UserId, request));

    [HttpGet("orders")]
    [RequireRole(Roles.Admin)]
    public IActionResult Orders([FromQuery] string? status) => Ok(store.GetOrders(status));

    [HttpGet("orders/{id:int}")]
    [RequireRole(Roles.Admin, Roles.Worker)]
    public IActionResult Order(int id) => ToResult(store.GetOrder(id, HttpContext.Session()));

    [HttpPost("orders/{id:int}/cancel")]
    [RequireRole(Roles.Admin)]
    public IActionResult Cancel(int id) => ToResult(store.Cancel(id));

    [HttpPut("orders/{id:int}/lines/{lineId:int}/worker")]
    [RequireRole(Roles.Admin)]
    public IActionResult Reassign(int id, int lineId, ReassignWorkerRequest request) =>
        ToResult(store.Reassign(id, lineId, request.WorkerId));

    [HttpGet("my/orders")]
    [RequireRole(Roles.Worker)]
    public IActionResult MyOrders() => Ok(store.GetMyOrders(HttpContext.Session().UserId));

    [HttpPost("orders/{id:int}/lines/{lineId:int}/pick")]
    [RequireRole(Roles.Worker)]
    public IActionResult Pick(int id, int lineId, ConfirmPickRequest? request) =>
        ToResult(store.ConfirmPick(id, lineId, HttpContext.Session().UserId, request?.Barcode));

    [HttpPost("orders/{id:int}/handoff")]
    [RequireRole(Roles.Worker)]
    public IActionResult Handoff(int id) =>
        ToResult(store.ConfirmHandoff(id, HttpContext.Session().UserId));

    [HttpPost("inbound")]
    [RequireRole(Roles.Worker)]
    public IActionResult CreateInbound(CreateInboundRequest request) =>
        ToResult(store.CreateInbound(HttpContext.Session().UserId, request));

    [HttpGet("inbound")]
    [RequireRole(Roles.Admin, Roles.Worker)]
    public IActionResult Inbound() => Ok(store.GetInbound());

    [HttpPut("inbound/{id:int}/shelf")]
    [RequireRole(Roles.Admin)]
    public IActionResult AssignShelf(int id, AssignShelfRequest request) =>
        ToResult(store.AssignShelf(id, request.ShelfId));

    [HttpPost("inbound/{id:int}/place")]
    [RequireRole(Roles.Worker)]
    public IActionResult Place(int id) =>
        ToResult(store.PlaceInbound(id, HttpContext.Session().UserId));

    [HttpGet("map")]
    [RequireRole(Roles.Worker)]
    public IActionResult Map()
    {
        var url = $"{Request.Scheme}://{Request.Host}/map.svg";
        return Ok(store.GetMap(url));
    }

    [HttpGet("orders/{id:int}/route")]
    [RequireRole(Roles.Worker)]
    public IActionResult Route(int id, [FromQuery] int anchorShelfId) =>
        ToResult(store.GetRoute(id, HttpContext.Session().UserId, anchorShelfId));

    [HttpGet("devices/{deviceId}/leds")]
    public IActionResult Leds(string deviceId) => Ok(store.GetLeds(deviceId));

    private IActionResult ToResult<T>(StoreResult<T> result) =>
        result.IsSuccess
            ? StatusCode(result.Status, result.Value)
            : StatusCode(result.Status, result.Error);
}
