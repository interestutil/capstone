using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Api;
using SmartWarehouse.AdminWeb.Models;

namespace SmartWarehouse.AdminWeb.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class InboundController(WarehouseApiClient api) : Controller
{
    public async Task<IActionResult> Index() => View(await LoadAsync());

    public async Task<IActionResult> Snapshot() => PartialView("_InboundTable", await LoadAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int shelfId)
    {
        try
        {
            await api.AssignInboundShelfAsync(id, shelfId);
            TempData["Success"] = "Shelf assigned. The worker can now confirm placement.";
        }
        catch (WarehouseApiException ex)
        {
            TempData["Error"] = ex.DisplayMessage;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<InboundPageViewModel> LoadAsync() =>
        new()
        {
            Records = await api.GetInboundAsync(),
            Shelves = await api.GetShelvesAsync(),
            Products = await api.GetProductsAsync()
        };
}
