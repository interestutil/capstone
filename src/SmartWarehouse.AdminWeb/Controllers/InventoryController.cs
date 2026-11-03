using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Api;
using SmartWarehouse.AdminWeb.Models;

namespace SmartWarehouse.AdminWeb.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class InventoryController(WarehouseApiClient api) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await LoadAsync());
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = "Cannot reach the warehouse API.";
            return View(new InventoryPageViewModel());
        }
    }

    public async Task<IActionResult> Snapshot() => PartialView("_InventorySnapshot", await LoadAsync());

    private async Task<InventoryPageViewModel> LoadAsync() =>
        new()
        {
            Shelves = await api.GetShelvesAsync(),
            Totals = await api.GetInventoryAsync()
        };
}
