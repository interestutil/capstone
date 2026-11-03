using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.AdminWeb.Api;
using SmartWarehouse.AdminWeb.Models;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.AdminWeb.Controllers;

public sealed class AccountController(WarehouseApiClient api) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await api.LoginAsync(new LoginRequest(model.Email, model.Password));
            if (!string.Equals(result.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "This dashboard is for the warehouse admin. Workers use the Android app.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new(ClaimTypes.Name, result.FullName),
                new(ClaimTypes.Email, result.Email),
                new(ClaimTypes.Role, result.Role)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

            Response.Cookies.Append(JwtAuthHandler.TokenCookie, result.Token, new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Inventory");
        }
        catch (WarehouseApiException ex)
        {
            ModelState.AddModelError(string.Empty, ex.StatusCode == 401 ? "Invalid email or password." : ex.DisplayMessage);
            return View(model);
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(string.Empty, "Cannot reach the warehouse API. Start the API (or the mock API) and try again.");
            return View(model);
        }
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        Response.Cookies.Delete(JwtAuthHandler.TokenCookie);
        return RedirectToAction(nameof(Login));
    }
}
