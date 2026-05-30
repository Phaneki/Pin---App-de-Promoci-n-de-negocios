using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IPhotoService, PhotoService>();
// 🔐 AUTENTICACIÓN (Google + Cookies)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie();

// Solo agregar Google si existen credenciales configuradas
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;

        options.Scope.Add("profile"); // 🔥 IMPORTANTE

        options.ClaimActions.MapJsonKey("picture", "picture", "url");
        options.ClaimActions.MapJsonKey("name", "name");

        options.SaveTokens = true;
    });
}

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddDbContext<PinDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// 1. Configurar Redis como el motor de Caché Distribuido
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "PinApp_";
    });
}

// 2. Agregar Sesiones DESPUÉS de Redis para que use la caché distribuida
builder.Services.AddSession();

// 3. Registrar servicios personalizados
builder.Services.AddHttpClient<OverpassService>();
builder.Services.AddScoped<OverpassService>();

// 4. Registrar NominatimService con HttpClient y timeout
builder.Services.AddHttpClient<NominatimService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });
builder.Services.AddScoped<NominatimService>();

// 5. Registrar BusinessHoursService
builder.Services.AddScoped<IBusinessHoursService, BusinessHoursService>();

var app = builder.Build();

// Ejecutar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try {
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Usuarios\" ADD COLUMN \"Ubicacion\" text;");
        dbContext.Database.ExecuteSqlRaw("ALTER TABLE \"Usuarios\" ADD COLUMN \"Bio\" text;");
    } catch { }
    dbContext.Database.Migrate();
    
    var pinContext = scope.ServiceProvider.GetRequiredService<PinDbContext>();
    pinContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
