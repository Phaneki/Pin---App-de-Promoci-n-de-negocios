using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using System.Dynamic;

namespace PinAppdePromo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;
        private readonly OverpassService _overpassService;

        public HomeController(AppDbContext context, PinDbContext pinContext, OverpassService overpassService)
        {
            _context = context;
            _pinContext = pinContext;
            _overpassService = overpassService;
        }

        public async Task<IActionResult> Index()
        {
            var negocios = await _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Where(b => b.Status == "Approved" || b.Status == "Promoted")
                .OrderByDescending(b => b.Status == "Promoted" ? 1 : 0)
                .ThenByDescending(b => b.CreatedAt)
                .Take(8)
                .ToListAsync();
            return View(negocios);
        }

        public async Task<IActionResult> Explorar(string busqueda, string distrito, List<int> categorias, string orden)
        {
            var query = _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Where(b => b.Status == "Approved" || b.Status == "Promoted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(b => b.TradeName.ToLower().Contains(busqueda.ToLower()) || b.Category.Name.ToLower().Contains(busqueda.ToLower()));
            if (!string.IsNullOrEmpty(distrito))
                query = query.Where(b => b.Address.ToLower().Contains(distrito.ToLower()));
            if (categorias != null && categorias.Any())
                query = query.Where(b => categorias.Contains(b.CategoryId));

            query = orden switch
            {
                "calificacion" => query.OrderByDescending(b => b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0),
                "nombre" => query.OrderBy(b => b.TradeName),
                _ => query.OrderByDescending(b => b.Status == "Promoted" ? 1 : 0)
            };

            var negocios = await query.ToListAsync();
            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            ViewBag.CategoriasSeleccionadas = categorias ?? new List<int>();
            ViewBag.OrdenActual = orden;
            ViewBag.DistritoActual = distrito;

            var todosNegocios = await _pinContext.Businesses
                .Where(b => b.Status == "Approved" || b.Status == "Promoted")
                .Select(b => b.Address)
                .ToListAsync();
            var distritosLima = new List<string> {
                "Ancón", "Ate", "Barranco", "Breña", "Carabayllo", "Chaclacayo", "Chorrillos", "Cieneguilla", 
                "Comas", "El Agustino", "Independencia", "Jesús María", "La Molina", "La Victoria", "Lince", 
                "Los Olivos", "Lurigancho", "Lurín", "Magdalena del Mar", "Miraflores", "Pachacámac", "Pucusana", 
                "Pueblo Libre", "Puente Piedra", "Punta Hermosa", "Punta Negra", "Rímac", "San Bartolo", 
                "San Borja", "San Isidro", "San Juan de Lurigancho", "San Juan de Miraflores", "San Luis", 
                "San Martín de Porres", "San Miguel", "Santa Anita", "Santa María del Mar", "Santa Rosa", 
                "Santiago de Surco", "Surco", "Surquillo", "Villa El Salvador", "Villa María del Triunfo",
                "Cercado de Lima", "Lima", "Callao", "Bellavista", "Carmen de la Legua", "La Perla", "La Punta", "Ventanilla", "Mi Perú"
            };

            var distritosEncontrados = new HashSet<string>();
            foreach (var addr in todosNegocios.Where(a => !string.IsNullOrEmpty(a)))
            {
                var upperAddr = addr.ToUpper();
                foreach (var d in distritosLima)
                {
                    if (upperAddr.Contains(d.ToUpper()))
                    {
                        distritosEncontrados.Add(d);
                    }
                }
            }

            ViewBag.Distritos = distritosEncontrados.OrderBy(d => d).ToList();
            return View(negocios);
        }



        public async Task<IActionResult> GenerarDatosDePrueba()
        {
            if (!await _context.Usuarios.AnyAsync(u => u.Correo == "lucia@gmail.com"))
            {
                _context.Usuarios.AddRange(
                    new Usuario { Nombre = "Lucía Méndez", Correo = "lucia@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Lucia+Mendez&background=28a745&color=fff" },
                    new Usuario { Nombre = "Carlos Rivera", Correo = "carlos@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Carlos+Rivera&background=0D8ABC&color=fff" }
                );
                await _context.SaveChangesAsync();
            }
            if (!await _pinContext.Roles.AnyAsync()) { _pinContext.Roles.Add(new Role { Name = "CLIENTE" }); await _pinContext.SaveChangesAsync(); }
            var rol = await _pinContext.Roles.FirstOrDefaultAsync();
            if (!await _pinContext.Users.AnyAsync(u => u.Email == "lucia@gmail.com"))
            {
                _pinContext.Users.AddRange(new User { Email = "lucia@gmail.com", FullName = "Lucía Méndez", PasswordHash = "123", RoleId = rol!.RoleId }, new User { Email = "carlos@gmail.com", FullName = "Carlos Rivera", PasswordHash = "123", RoleId = rol!.RoleId });
                await _pinContext.SaveChangesAsync();
            }
            if (!await _pinContext.Categories.AnyAsync())
            {
                _pinContext.Categories.AddRange(new Category { Name = "Restaurantes" }, new Category { Name = "Tecnología" }, new Category { Name = "Servicios Automotrices" }, new Category { Name = "Salud y Belleza" });
                await _pinContext.SaveChangesAsync();
            }
            var pinUserLucia = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == "lucia@gmail.com");
            var pinUserCarlos = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == "carlos@gmail.com");
            var catRestaurantes = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Restaurantes");
            var catTecnologia = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Tecnología");
            var catServicios = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Servicios Automotrices");
            if (!await _pinContext.Businesses.AnyAsync(b => b.TradeName == "Cevichería Punto Azul"))
            {
                var b1 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catRestaurantes!.CategoryId, TradeName = "Cevichería Punto Azul", Description = "Los mejores pescados y mariscos frescos del día.", Address = "Calle San Martín 595, Miraflores", Latitude = (decimal)-12.1245, Longitude = (decimal)-77.0250, ContactPhone = "987654321", Status = "Promoted", CreatedAt = DateTime.UtcNow };
                var b2 = new Business { OwnerId = pinUserCarlos!.UserId, CategoryId = catTecnologia!.CategoryId, TradeName = "TechCenter Lima", Description = "Venta de laptops y accesorios gamer.", Address = "Av. Arenales 1234, San Isidro", Latitude = (decimal)-12.0833, Longitude = (decimal)-77.0355, ContactPhone = "999888777", Status = "Approved", CreatedAt = DateTime.UtcNow };
                var b3 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catServicios!.CategoryId, TradeName = "Taller FastFix", Description = "Mantenimiento y pintura automotriz.", Address = "Av. Santiago de Surco 456, Surco", Latitude = (decimal)-12.1388, Longitude = (decimal)-76.9989, ContactPhone = "912345678", Status = "Approved", CreatedAt = DateTime.UtcNow };
                _pinContext.Businesses.AddRange(b1, b2, b3);
                await _pinContext.SaveChangesAsync();
                _pinContext.BusinessImages.AddRange(new BusinessImage { BusinessId = b1.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1559314809-0d155014e29e?w=800&q=80" }, new BusinessImage { BusinessId = b2.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1531297172869-c7d6b8b82922?w=800&q=80" }, new BusinessImage { BusinessId = b3.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1613214149922-f1809c99b414?w=800&q=80" });
                _pinContext.Reviews.AddRange(new Review { BusinessId = b1.BusinessId, UserId = pinUserCarlos.UserId, Rating = 5, Comment = "¡Excelente!", CreatedAt = DateTime.UtcNow.AddDays(-2) }, new Review { BusinessId = b1.BusinessId, UserId = pinUserLucia.UserId, Rating = 4, Comment = "Muy bueno", CreatedAt = DateTime.UtcNow.AddDays(-1) }, new Review { BusinessId = b2.BusinessId, UserId = pinUserLucia.UserId, Rating = 5, Comment = "Buen servicio", CreatedAt = DateTime.UtcNow });
                await _pinContext.SaveChangesAsync();
            }
            return Content("¡ÉXITO! Base de datos poblada.");
        }
    }
}