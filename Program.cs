using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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

    // Auto-Seeding de Datos de Prueba (Usuarios, Categorías y Negocios de prueba)
    if (!dbContext.Usuarios.Any(u => u.Correo == "lucia@gmail.com"))
    {
        dbContext.Usuarios.AddRange(
            new Usuario { Nombre = "Lucía Méndez", Correo = "lucia@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Lucia+Mendez&background=28a745&color=fff" },
            new Usuario { Nombre = "Carlos Rivera", Correo = "carlos@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Carlos+Rivera&background=0D8ABC&color=fff" }
        );
        dbContext.SaveChanges();
    }

    if (!pinContext.Roles.Any()) { pinContext.Roles.Add(new Role { Name = "CLIENTE" }); pinContext.SaveChanges(); }
    var rol = pinContext.Roles.FirstOrDefault();

    if (!pinContext.Users.Any(u => u.Email == "lucia@gmail.com"))
    {
        pinContext.Users.AddRange(
            new User { Email = "lucia@gmail.com", FullName = "Lucía Méndez", PasswordHash = "123", RoleId = rol!.RoleId }, 
            new User { Email = "carlos@gmail.com", FullName = "Carlos Rivera", PasswordHash = "123", RoleId = rol!.RoleId }
        );
        pinContext.SaveChanges();
    }

    if (!pinContext.Categories.Any())
    {
        pinContext.Categories.AddRange(
            new Category { Name = "Restaurantes" }, new Category { Name = "Tecnología" }, 
            new Category { Name = "Servicios Automotrices" }, new Category { Name = "Salud y Belleza" }
        );
        pinContext.SaveChanges();
    }

    var pinUserLucia = pinContext.Users.FirstOrDefault(u => u.Email == "lucia@gmail.com");
    var pinUserCarlos = pinContext.Users.FirstOrDefault(u => u.Email == "carlos@gmail.com");
    var catRestaurantes = pinContext.Categories.FirstOrDefault(c => c.Name == "Restaurantes");
    var catTecnologia = pinContext.Categories.FirstOrDefault(c => c.Name == "Tecnología");
    var catServicios = pinContext.Categories.FirstOrDefault(c => c.Name == "Servicios Automotrices");

    var b1 = pinContext.Businesses.FirstOrDefault(b => b.TradeName == "Cevichería Punto Azul");
    var b2 = pinContext.Businesses.FirstOrDefault(b => b.TradeName == "TechCenter Lima");
    var b3 = pinContext.Businesses.FirstOrDefault(b => b.TradeName == "Taller FastFix");

    if (b1 == null && pinUserLucia != null && pinUserCarlos != null && catRestaurantes != null && catTecnologia != null && catServicios != null)
    {
        b1 = new Business { OwnerId = pinUserLucia.UserId, CategoryId = catRestaurantes.CategoryId, TradeName = "Cevichería Punto Azul", Description = "Los mejores pescados y mariscos frescos del día.", Address = "Calle San Martín 595, Miraflores", Latitude = (decimal)-12.1245, Longitude = (decimal)-77.0250, ContactPhone = "987654321", Status = "Promoted", CreatedAt = DateTime.UtcNow };
        b2 = new Business { OwnerId = pinUserCarlos.UserId, CategoryId = catTecnologia.CategoryId, TradeName = "TechCenter Lima", Description = "Venta de laptops y accesorios gamer.", Address = "Av. Arenales 1234, San Isidro", Latitude = (decimal)-12.0833, Longitude = (decimal)-77.0355, ContactPhone = "999888777", Status = "Approved", CreatedAt = DateTime.UtcNow };
        b3 = new Business { OwnerId = pinUserLucia.UserId, CategoryId = catServicios.CategoryId, TradeName = "Taller FastFix", Description = "Mantenimiento y pintura automotriz.", Address = "Av. Santiago de Surco 456, Surco", Latitude = (decimal)-12.1388, Longitude = (decimal)-76.9989, ContactPhone = "912345678", Status = "Approved", CreatedAt = DateTime.UtcNow };
        pinContext.Businesses.AddRange(b1, b2, b3);
        pinContext.SaveChanges();
    }

    if (b1 != null && !pinContext.BusinessImages.Any(i => i.BusinessId == b1.BusinessId))
    {
        // NO agregar imágenes ficticias - los dueños subirán sus propias imágenes
    }
    if (b2 != null && !pinContext.BusinessImages.Any(i => i.BusinessId == b2.BusinessId))
    {
        // NO agregar imágenes ficticias - los dueños subirán sus propias imágenes
    }
    if (b3 != null && !pinContext.BusinessImages.Any(i => i.BusinessId == b3.BusinessId))
    {
        // NO agregar imágenes ficticias - los dueños subirán sus propias imágenes
    }

    // UPDATE ALL DEAD UNSPLASH AND PLACEHOLD.CO URLS - DELETE INSTEAD
    var deadImages = pinContext.BusinessImages.Where(i => 
        i.ImageUrl.Contains("unsplash.com") || 
        i.ImageUrl.Contains("placehold.co") ||
        i.ImageUrl.Contains("picsum.photos") ||
        i.ImageUrl.Contains("default-")).ToList();
    if (deadImages.Any())
    {
        pinContext.BusinessImages.RemoveRange(deadImages);
        pinContext.SaveChanges();
    }
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
