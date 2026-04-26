using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using System.Dynamic;

namespace PinAppdePromo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;

        public HomeController(AppDbContext context, PinDbContext pinContext)
        {
            _context = context;
            _pinContext = pinContext;
        }

        public async Task<IActionResult> Index()
        {
            var negocios = await _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Where(b => b.Status == "Approved") // Solo muestra los aprobados
                .Take(8) // Limitamos a los 8 más recientes en el inicio
                .ToListAsync();
            return View(negocios);
        }

        public async Task<IActionResult> Explorar(string busqueda, string distrito)
        {
            var query = _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Where(b => b.Status == "Approved")
                .AsQueryable();

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(b => b.TradeName.ToLower().Contains(busqueda.ToLower()) || b.Category.Name.ToLower().Contains(busqueda.ToLower()));
            }

            if (!string.IsNullOrEmpty(distrito))
            {
                query = query.Where(b => b.Address.ToLower().Contains(distrito.ToLower()));
            }
                
            var negocios = await query.ToListAsync();
            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            
            return View(negocios);
        }

        public async Task<IActionResult> InfNegocio(int id)
        {
            var negocio = await _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(n => n.BusinessId == id);

            if (negocio == null)
            {
                return NotFound();
            }

            return View(negocio);
        }
        public async Task<IActionResult> RegistrarNegocio()
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearNegocio(Business negocio, List<IFormFile> Imagenes)
        {
            negocio.Status = "Pending";
            negocio.CreatedAt = DateTime.UtcNow;

            // Obtener el OwnerId basado en el usuario logueado en la sesión
            var email = HttpContext.Session.GetString("Usuario");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            
            // Si el usuario existe se asocia, sino le ponemos 1 temporalmente como fallback
            negocio.OwnerId = user?.Id ?? 1; 

            _pinContext.Businesses.Add(negocio);
            await _pinContext.SaveChangesAsync();

            // Simulación del guardado de imágenes
            if (Imagenes != null && Imagenes.Count > 0)
            {
                foreach (var img in Imagenes)
                {
                    _pinContext.BusinessImages.Add(new BusinessImage
                    {
                        BusinessId = negocio.BusinessId,
                        ImageUrl = $"/images/temp_{Guid.NewGuid()}_{img.FileName}"
                    });
                }
                await _pinContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Moderacion()
        {
            if (HttpContext.Session.GetString("Usuario") != "admin@pin.com")
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ModeracionViewModel
            {
                SolicitudesPendientes = await _pinContext.Businesses.CountAsync(b => b.Status == "Pending"),
                NegociosAprobadosHoy = await _pinContext.Businesses.CountAsync(b => b.Status == "Approved" && b.CreatedAt.Date == DateTime.UtcNow.Date),
                TasaRechazo = 5.2,
                DenunciasPendientes = await _pinContext.BusinessReports
                    .Include(r => r.Business).ThenInclude(b => b.Category)
                    .Include(r => r.Business).ThenInclude(b => b.Images) // Corregido: Uso correcto de ThenInclude
                    .Where(r => r.ReportStatus == "Open")
                    .ToListAsync(),
                ActividadReciente = await _pinContext.StaffLogs
                    .Include(l => l.Staff)
                    .OrderByDescending(l => l.ExecutedAt)
                    .Take(4)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Perfil()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            
            // Usamos ExpandoObject para enviar las variables de forma dinámica sin romper la vista
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow; // O usa la fecha de creación real si la tienes
                
                // Cargar los favoritos reales del usuario
                model.Favorites = await _pinContext.Favorites
                    .Include(f => f.Business).ThenInclude(b => b.Category)
                    .Include(f => f.Business).ThenInclude(b => b.Images)
                    .Where(f => f.UserId == user.Id)
                    .ToListAsync();
            }
                
            return View("~/Views/Home/Perfil/Index.cshtml", model);
        }

        public async Task<IActionResult> MisResenas()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                
                model.Reviews = await _pinContext.Reviews
                    .Include(r => r.Business)
                    .Where(r => r.UserId == user.Id) // Filtra las reseñas por el Id del usuario
                    .ToListAsync();
            }
                
            return View("~/Views/Home/Perfil/MisResenas.cshtml", model);
        }

        public async Task<IActionResult> AjustesCuenta()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null)
            {
                return RedirectToAction("Index", "Login");
            }
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
            }
            return View("~/Views/Home/Perfil/AjustesCuenta.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPerfil(string FullName, string Bio)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null)
            {
                user.Nombre = FullName; // Ahora sí actualiza el Nombre del modelo Usuario
                // user.Bio = Bio; 
                await _context.SaveChangesAsync();
                
                // Actualizamos la sesión para que el cambio de nombre se vea inmediatamente
                HttpContext.Session.SetString("Nombre", FullName);
            }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            if (NewPassword != ConfirmPassword)
            {
                return RedirectToAction("AjustesCuenta"); // Puedes añadir un mensaje de error aquí
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null && user.Password == CurrentPassword) // Aquí puedes usar Hash si lo implementaste
            {
                user.Password = NewPassword;
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarCuenta()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null)
            {
                _context.Usuarios.Remove(user);
                await _context.SaveChangesAsync();
                
                HttpContext.Session.Clear(); // Cerramos la sesión
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> AgregarResena(int BusinessId, int Rating, string Comment)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Index", "Login");
            }

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user == null) return RedirectToAction("Index", "Login");

            var review = new Review
            {
                BusinessId = BusinessId,
                UserId = user.Id,
                Rating = Rating,
                Comment = Comment,
                CreatedAt = DateTime.UtcNow
            };

            _pinContext.Reviews.Add(review);
            await _pinContext.SaveChangesAsync();

            return RedirectToAction("InfNegocio", new { id = BusinessId });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarResena(int reviewId)
        {
            var review = await _pinContext.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                _pinContext.Reviews.Remove(review);
                await _pinContext.SaveChangesAsync();
            }
            return RedirectToAction("MisResenas");
        }

        // ==========================================
        // MÉTODO TEMPORAL PARA POBLAR BASE DE DATOS
        // ==========================================
        public async Task<IActionResult> GenerarDatosDePrueba()
        {
            // 1. Agregar Usuarios si no existen
            if (!await _context.Usuarios.AnyAsync(u => u.Correo == "lucia@gmail.com"))
            {
                _context.Usuarios.AddRange(
                    new Usuario { Nombre = "Lucía Méndez", Correo = "lucia@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Lucia+Mendez&background=28a745&color=fff" },
                    new Usuario { Nombre = "Carlos Rivera", Correo = "carlos@gmail.com", Password = "123", Rol = "CLIENTE", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Carlos+Rivera&background=0D8ABC&color=fff" }
                );
                await _context.SaveChangesAsync();
            }

            // 2. Agregar Categorías si no existen
            if (!await _pinContext.Categories.AnyAsync())
            {
                _pinContext.Categories.AddRange(
                    new Category { Name = "Restaurantes" },
                    new Category { Name = "Tecnología" },
                    new Category { Name = "Servicios Automotrices" },
                    new Category { Name = "Salud y Belleza" }
                );
                await _pinContext.SaveChangesAsync();
            }

            // Obtenemos los IDs generados automáticamente para armar las relaciones
            var userLucia = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == "lucia@gmail.com");
            var userCarlos = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == "carlos@gmail.com");
            var catRestaurantes = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Restaurantes");
            var catTecnologia = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Tecnología");
            var catServicios = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Servicios Automotrices");

            // 3. Agregar Negocios y Relaciones
            if (!await _pinContext.Businesses.AnyAsync(b => b.TradeName == "Cevichería Punto Azul"))
            {
                var b1 = new Business { OwnerId = userLucia!.Id, CategoryId = catRestaurantes!.CategoryId, TradeName = "Cevichería Punto Azul", Description = "Los mejores pescados y mariscos frescos del día. Especialidad en ceviche carretillero y jalea mixta.", Address = "Calle San Martín 595, Miraflores", Latitude = (decimal)-12.1245, Longitude = (decimal)-77.0250, ContactPhone = "987654321", Status = "Promoted", CreatedAt = DateTime.UtcNow };
                var b2 = new Business { OwnerId = userCarlos!.Id, CategoryId = catTecnologia!.CategoryId, TradeName = "TechCenter Lima", Description = "Venta de laptops, accesorios gamer y servicio técnico especializado para computadoras.", Address = "Av. Arenales 1234, San Isidro", Latitude = (decimal)-12.0833, Longitude = (decimal)-77.0355, ContactPhone = "999888777", Status = "Approved", CreatedAt = DateTime.UtcNow };
                var b3 = new Business { OwnerId = userLucia.Id, CategoryId = catServicios!.CategoryId, TradeName = "Taller FastFix", Description = "Mantenimiento preventivo, afinamiento, planchado y pintura automotriz.", Address = "Av. Santiago de Surco 456, Surco", Latitude = (decimal)-12.1388, Longitude = (decimal)-76.9989, ContactPhone = "912345678", Status = "Approved", CreatedAt = DateTime.UtcNow };

                _pinContext.Businesses.AddRange(b1, b2, b3);
                await _pinContext.SaveChangesAsync();

                _pinContext.BusinessImages.AddRange(
                    new BusinessImage { BusinessId = b1.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1559314809-0d155014e29e?w=800&q=80" },
                    new BusinessImage { BusinessId = b2.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1531297172869-c7d6b8b82922?w=800&q=80" },
                    new BusinessImage { BusinessId = b3.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1613214149922-f1809c99b414?w=800&q=80" }
                );

                _pinContext.Reviews.AddRange(
                    new Review { BusinessId = b1.BusinessId, UserId = userCarlos.Id, Rating = 5, Comment = "¡El mejor ceviche que he probado en Miraflores! Atención rápida y porciones generosas.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new Review { BusinessId = b1.BusinessId, UserId = userLucia.Id, Rating = 4, Comment = "Muy rico todo, aunque el local estaba un poco lleno por ser fin de semana.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Review { BusinessId = b2.BusinessId, UserId = userLucia.Id, Rating = 5, Comment = "Compré una laptop para la universidad y me asesoraron súper bien. Precios justos.", CreatedAt = DateTime.UtcNow }
                );

                await _pinContext.SaveChangesAsync();
            }

            return Content("¡ÉXITO! Tu base de datos ha sido poblada con negocios reales de Lima, fotos, reseñas y usuarios. Ya puedes regresar al Inicio y probar la plataforma.");
        }
    }
}
