using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Contracts;

namespace SmartWarehouse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));
}
