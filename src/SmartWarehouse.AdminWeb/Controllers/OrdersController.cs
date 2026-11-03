using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Api;
using SmartWarehouse.AdminWeb.Models;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.AdminWeb.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class OrdersController(WarehouseApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? status)
    {
        var orders = await api.GetOrdersAsync(status);
        return View(new OrderListViewModel { Status = status, Orders = orders });
    }

    public async Task<IActionResult> Snapshot(string? status) =>
        PartialView("_OrderTable", await api.GetOrdersAsync(status));

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            return View(new OrderDetailPageViewModel
            {
                Order = await api.GetOrderAsync(id),
                Workers = await api.GetWorkersAsync()
            });
        }
        catch (WarehouseApiException ex) when (ex.StatusCode == 404)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await Populate(new CreateOrderViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        await Populate(model);
        var lines = model.Lines
            .Where(l => l.ProductId > 0 && l.ShelfId > 0 && l.AssignedWorkerId > 0 && l.QtyCartons > 0)
            .Select(l => new CreateOrderLineRequest(l.ShelfId, l.ProductId, l.QtyCartons, l.AssignedWorkerId))
            .ToList();

        if (lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Add at least one complete order line.");
            return View(model);
        }

        try
        {
            var created = await api.CreateOrderAsync(new CreateOrderRequest(model.BranchId, model.Deadline.ToUniversalTime(), lines));
            TempData["Success"] = $"Order #{created.Id} was created and stock was committed.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (WarehouseApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.DisplayMessage);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await api.CancelOrderAsync(id);
            TempData["Success"] = "Order cancelled. Committed stock was released.";
        }
        catch (WarehouseApiException ex)
        {
            TempData["Error"] = ex.DisplayMessage;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(int id, int lineId, int workerId)
    {
        try
        {
            await api.ReassignLineAsync(id, lineId, workerId);
            TempData["Success"] = "Line reassigned.";
        }
        catch (WarehouseApiException ex)
        {
            TempData["Error"] = ex.DisplayMessage;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<CreateOrderViewModel> Populate(CreateOrderViewModel model)
    {
        model.Branches = await api.GetBranchesAsync();
        model.Products = await api.GetProductsAsync();
        model.Shelves = await api.GetShelvesAsync();
        model.Workers = await api.GetWorkersAsync();
        if (model.Lines.Count == 0) model.Lines.Add(new CreateOrderLineForm());
        return model;
    }
}
