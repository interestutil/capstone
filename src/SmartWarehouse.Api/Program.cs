using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartWarehouse.Api.Errors;
using SmartWarehouse.Api.Services;
using SmartWarehouse.Domain.Entities;
using SmartWarehouse.Infrastructure.Data;
using SmartWarehouse.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Warehouse Core API", Version = "v2.0" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddExceptionHandler<WarehouseExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();
builder.Services.AddScoped<IShelfRepository, ShelfRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<InboundService>();
builder.Services.AddScoped<RoutingService>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("Warehouse")
                           ?? "Server=(localdb)\\mssqllocaldb;Database=SmartWarehouse;Trusted_Connection=True;TrustServerCertificate=True";

    builder.Services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(connectionString));
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-change-this-key-32chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartWarehouse";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new SmartWarehouse.Contracts.ApiError("Sign in required."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new SmartWarehouse.Contracts.ApiError("You do not have access to this action."));
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    if (app.Environment.IsEnvironment("Testing"))
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
    await WarehouseSeed.EnsureUsersAsync(db);
}

app.Run();

public partial class Program;
