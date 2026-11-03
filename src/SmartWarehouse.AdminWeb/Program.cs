using Microsoft.AspNetCore.Authentication.Cookies;
using SmartWarehouse.AdminWeb.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtAuthHandler>();
builder.Services.AddHttpClient<WarehouseApiClient>((sp, client) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddHttpMessageHandler<JwtAuthHandler>();

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "wms.admin";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Inventory}/{action=Index}/{id?}");
app.Run();
