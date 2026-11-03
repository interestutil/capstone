using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<ActionResult<List<BranchDto>>> Branches(CancellationToken ct) =>
        Ok(await catalog.GetBranchesAsync(ct));

    [HttpGet("products")]
    public async Task<ActionResult<List<ProductDto>>> Products(CancellationToken ct) =>
        Ok(await catalog.GetProductsAsync(ct));

    [Authorize(Roles = Roles.Worker)]
    [HttpGet("products/by-barcode/{code}")]
    public async Task<ActionResult<ProductDto>> ProductByBarcode(string code, CancellationToken ct) =>
        Ok(await catalog.GetProductByBarcodeAsync(code, ct));

    [HttpGet("shelves")]
    public async Task<ActionResult<List<ShelfDto>>> Shelves(CancellationToken ct) =>
        Ok(await catalog.GetShelvesAsync(ct));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("inventory")]
    public async Task<ActionResult<List<InventoryRowDto>>> Inventory(CancellationToken ct) =>
        Ok(await catalog.GetInventoryAsync(ct));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("workers")]
    public async Task<ActionResult<List<WorkerDto>>> Workers(CancellationToken ct) =>
        Ok(await catalog.GetWorkersAsync(ct));
}
