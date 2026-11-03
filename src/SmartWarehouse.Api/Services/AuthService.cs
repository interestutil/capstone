using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SmartWarehouse.Api.Errors;
using SmartWarehouse.Contracts;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Repositories;

namespace SmartWarehouse.Api.Services;

public sealed class AuthService(
    IWarehouseRepository repo,
    IPasswordHasher<User> passwordHasher,
    IConfiguration config)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = await repo.FindUserByEmailAsync(email, ct);
        if (user is null || !user.IsActive)
            throw WarehouseException.Unauthorized("Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw WarehouseException.Unauthorized("Invalid email or password.");

        var secret = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = config["Jwt:Issuer"] ?? "SmartWarehouse";
        var lifetimeHours = int.TryParse(config["Jwt:LifetimeHours"], out var hours) ? hours : 12;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer,
            issuer,
            claims,
            expires: DateTime.UtcNow.AddHours(lifetimeHours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse(jwt, user.Id, user.FullName, user.Role, user.Email);
    }
}
