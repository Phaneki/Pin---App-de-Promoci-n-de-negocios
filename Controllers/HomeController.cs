using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;
using PinAppdePromo.ML;
using System.Dynamic;

namespace PinAppdePromo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;
        private readonly OverpassService _overpassService;
        private readonly RecommendationAnalysisService _recommendationAnalysisService;
        private readonly IPhotoService _photoService;
        private readonly IGooglePlacesService _googlePlacesService;

        public HomeController(AppDbContext context, PinDbContext pinContext, OverpassService overpassService, RecommendationAnalysisService recommendationAnalysisService, IPhotoService photoService, IGooglePlacesService googlePlacesService)
        {
            _context = context;
            _pinContext = pinContext;
            _overpassService = overpassService;
            _recommendationAnalysisService = recommendationAnalysisService;
            _photoService = photoService;
            _googlePlacesService = googlePlacesService;
        }

        public IActionResult Beneficios()
        {
            return View();
        }

        public IActionResult Privacidad()
        {
            return View();
        }

        public IActionResult Nosotros()
        {
            return View();
        }

        public IActionResult Contacto()
        {
            return View();
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

            // 📸 Auto-sincronizar imágenes desde Google Places si faltan en la página de inicio
            bool hasNewImages = false;
            foreach (var negocio in negocios)
            {
                bool needsImage = !negocio.Images.Any() || negocio.Images.Any(img => img.ImageUrl.Contains("ui-avatars.com"));
                if (needsImage)
                {
                    var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(negocio.TradeName, negocio.Address);
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        bool isRealImage = !photoUrl.Contains("ui-avatars.com");
                        if (isRealImage)
                        {
                            var fallbacks = negocio.Images.Where(img => img.ImageUrl.Contains("ui-avatars.com")).ToList();
                            if (fallbacks.Any()) { _pinContext.BusinessImages.RemoveRange(fallbacks); foreach(var f in fallbacks) negocio.Images.Remove(f); }
                            
                            var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                            _pinContext.BusinessImages.Add(nuevaImagen);
                            negocio.Images.Add(nuevaImagen);
                            hasNewImages = true;
                        }
                        else if (!negocio.Images.Any())
                        {
                            var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                            _pinContext.BusinessImages.Add(nuevaImagen);
                            negocio.Images.Add(nuevaImagen);
                            hasNewImages = true;
                        }
                    }
                }
            }
            if (hasNewImages) await _pinContext.SaveChangesAsync();

            var resenasRecientes = await _pinContext.Reviews
                .Include(r => r.User)
                .Include(r => r.Business)
                .Where(r => r.Rating >= 4 && r.Comment != null && r.Comment != "")
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.ResenasRecientes = resenasRecientes;

            return View(negocios);
        }

        [HttpGet]
        public IActionResult DebugImages()
        {
            var data = _pinContext.Businesses
                .Include(b => b.Images)
                .Select(b => new { 
                    b.BusinessId, 
                    b.TradeName, 
                    ImageCount = b.Images.Count, 
                    Images = b.Images.Select(i => i.ImageUrl).ToList() 
                })
                .ToList();
            return Json(data);
        }

        public async Task<IActionResult> Explorar(string busqueda, string distrito, List<int> categorias, string orden, int page = 1)
        {
            const int pageSize = 10;
            
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

            // Obtener total antes de paginar
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Validar página
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Aplicar paginación
            int skip = (page - 1) * pageSize;
            var negocios = await query.Skip(skip).Take(pageSize).ToListAsync();

            // 📸 Auto-sincronizar imágenes desde Google Places para los negocios en la página actual
            bool hasNewImages = false;
            foreach (var negocio in negocios)
            {
                bool needsImage = !negocio.Images.Any() || negocio.Images.Any(img => img.ImageUrl.Contains("ui-avatars.com"));
                if (needsImage)
                {
                    var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(negocio.TradeName, negocio.Address);
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        bool isRealImage = !photoUrl.Contains("ui-avatars.com");
                        if (isRealImage)
                        {
                            var fallbacks = negocio.Images.Where(img => img.ImageUrl.Contains("ui-avatars.com")).ToList();
                            if (fallbacks.Any()) { _pinContext.BusinessImages.RemoveRange(fallbacks); foreach(var f in fallbacks) negocio.Images.Remove(f); }
                            
                            var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                            _pinContext.BusinessImages.Add(nuevaImagen);
                            negocio.Images.Add(nuevaImagen);
                            hasNewImages = true;
                        }
                        else if (!negocio.Images.Any())
                        {
                            var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                            _pinContext.BusinessImages.Add(nuevaImagen);
                            negocio.Images.Add(nuevaImagen);
                            hasNewImages = true;
                        }
                    }
                }
            }
            if (hasNewImages) await _pinContext.SaveChangesAsync();

            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            ViewBag.CategoriasSeleccionadas = categorias ?? new List<int>();
            ViewBag.OrdenActual = orden;
            ViewBag.DistritoActual = distrito;
            ViewBag.BusquedaActual = busqueda;

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
            
            ViewBag.Distritos = distritosLima.OrderBy(d => d).ToList();

            // Paginación
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Generar recomendaciones personalizadas si el usuario está autenticado
            var email = HttpContext.Session.GetString("Usuario");
            if (!string.IsNullOrEmpty(email))
            {
                var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (pinUser != null)
                {
                    var historial = await _pinContext.BusquedasUsuario.Where(b => b.UsuarioId == pinUser.UserId).ToListAsync();

                    // Obtener todos los negocios activos para la recomendación global
                    var todosLosNegocios = await _pinContext.Businesses
                        .Include(b => b.Category)
                        .Include(b => b.Images)
                        .Include(b => b.Reviews)
                        .Where(b => b.Status == "Approved" || b.Status == "Promoted")
                        .ToListAsync();

                    // Mapear TODOS los negocios a DTOs para el servicio de recomendación
                    var negociosDto = todosLosNegocios.Select(b => new ML.NegocioDTO
                    {
                        Id = b.BusinessId,
                        Nombre = b.TradeName ?? string.Empty,
                        Categoria = b.Category?.Name ?? string.Empty,
                        Direccion = b.Address ?? string.Empty,
                        Calificacion = b.Reviews != null && b.Reviews.Any() ? b.Reviews.Average(r => r.Rating) : 0,
                        ImagenUrl = b.Images?.FirstOrDefault()?.ImageUrl
                    }).ToList();

                    var recomendaciones = _recommendationAnalysisService.GetPersonalizedRecommendations(pinUser.UserId, historial, negociosDto, 10);
                    ViewBag.Recomendaciones = recomendaciones;
                }
            }

            return View(negocios);
        }

        public async Task<IActionResult> InfNegocio(int id)
        {
            var negocio = await _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(n => n.BusinessId == id);
            if (negocio == null) return NotFound();

            // 📸 Auto-sincronizar imagen desde Google Places si el negocio no tiene ninguna
            bool needsImage = !negocio.Images.Any() || negocio.Images.Any(img => img.ImageUrl.Contains("ui-avatars.com"));
            if (needsImage)
            {
                var photoUrl = await _googlePlacesService.GetBusinessPhotoUrlAsync(negocio.TradeName, negocio.Address);
                
                if (!string.IsNullOrEmpty(photoUrl))
                {
                    bool isRealImage = !photoUrl.Contains("ui-avatars.com");
                    if (isRealImage)
                    {
                        var fallbacks = negocio.Images.Where(img => img.ImageUrl.Contains("ui-avatars.com")).ToList();
                        if (fallbacks.Any()) { _pinContext.BusinessImages.RemoveRange(fallbacks); foreach(var f in fallbacks) negocio.Images.Remove(f); }
                        
                        var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                        _pinContext.BusinessImages.Add(nuevaImagen);
                        await _pinContext.SaveChangesAsync();
                        negocio.Images.Add(nuevaImagen);
                    }
                    else if (!negocio.Images.Any())
                    {
                        var nuevaImagen = new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = photoUrl };
                        _pinContext.BusinessImages.Add(nuevaImagen);
                        await _pinContext.SaveChangesAsync();
                        negocio.Images.Add(nuevaImagen);
                    }
                }
            }

            // 📞 Auto-sincronizar información adicional (teléfono, horarios, web) si faltan
            bool needsInfo = string.IsNullOrEmpty(negocio.ContactPhone) || string.IsNullOrEmpty(negocio.Description) || (!negocio.Description.Contains("Horarios"));
            if (needsInfo)
            {
                var info = await _googlePlacesService.GetBusinessInfoAsync(negocio.TradeName, negocio.Address);
                if (info != null)
                {
                    bool infoUpdated = false;
                    if (string.IsNullOrEmpty(negocio.ContactPhone) && !string.IsNullOrEmpty(info.PhoneNumber))
                    {
                        negocio.ContactPhone = info.PhoneNumber;
                        infoUpdated = true;
                    }
                    if (!string.IsNullOrEmpty(info.Website) && (string.IsNullOrEmpty(negocio.Description) || !negocio.Description.Contains(info.Website)))
                    {
                        negocio.Description = string.IsNullOrEmpty(negocio.Description) ? $"Sitio web: {info.Website}" : $"{negocio.Description} | Sitio web: {info.Website}";
                        infoUpdated = true;
                    }
                    if (!string.IsNullOrEmpty(info.OpeningHours) && (string.IsNullOrEmpty(negocio.Description) || !negocio.Description.Contains("Horarios")))
                    {
                        negocio.Description = string.IsNullOrEmpty(negocio.Description) ? $"Horarios: {info.OpeningHours}" : $"{negocio.Description} | Horarios: {info.OpeningHours}";
                        infoUpdated = true;
                    }
                    if (infoUpdated) await _pinContext.SaveChangesAsync();
                }
            }

            return View(negocio);
        }

        public async Task<IActionResult> RegistrarNegocio()
        {
            if (HttpContext.Session.GetString("Usuario") == null) return RedirectToAction("Index", "Login");
            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearNegocio(Business negocio, List<IFormFile> Imagenes, string ImageUrlLink)
        {
            try
            {
                negocio.Status = "Pending";
                negocio.CreatedAt = DateTime.UtcNow;
                negocio.RUC = negocio.RUC ?? "";
                negocio.ContactPhone = negocio.ContactPhone ?? "";
                var email = HttpContext.Session.GetString("Usuario");
                var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (pinUser == null && !string.IsNullOrEmpty(email))
                {
                    var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                    pinUser = new User { Email = email, FullName = localUser?.Nombre ?? "Usuario", PasswordHash = localUser?.Password ?? "", ProfilePic = "", RoleId = 1 };
                    _pinContext.Users.Add(pinUser);
                    await _pinContext.SaveChangesAsync();
                }
                negocio.OwnerId = pinUser?.UserId ?? 1;
                _pinContext.Businesses.Add(negocio);
                await _pinContext.SaveChangesAsync();
                if (!string.IsNullOrEmpty(ImageUrlLink))
                {
                    var secureUrl = await _photoService.SubirImagenPorUrlAsync(ImageUrlLink);
                    if (!string.IsNullOrEmpty(secureUrl))
                    {
                        _pinContext.BusinessImages.Add(new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = secureUrl });
                        await _pinContext.SaveChangesAsync();
                    }
                }
                if (Imagenes != null && Imagenes.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "businesses");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    foreach (var img in Imagenes)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(img.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await img.CopyToAsync(fileStream); }
                        _pinContext.BusinessImages.Add(new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = $"/images/businesses/{uniqueFileName}" });
                    }
                    await _pinContext.SaveChangesAsync();
                }
                TempData["Exito"] = "¡Tu negocio se ha registrado con éxito! Un moderador lo revisará pronto para publicarlo en la plataforma.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al registrar tu negocio. Inténtalo de nuevo.";
                return RedirectToAction("RegistrarNegocio");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Moderacion()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            // Sync profile picture to PinDbContext
            var email = HttpContext.Session.GetString("Usuario");
            var foto = HttpContext.Session.GetString("Foto");
            if (!string.IsNullOrEmpty(email))
            {
                var currentUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (currentUser != null && !string.IsNullOrEmpty(foto) && currentUser.ProfilePic != foto)
                {
                    currentUser.ProfilePic = foto;
                    await _pinContext.SaveChangesAsync();
                }
            }

            var totalResueltos = await _pinContext.Businesses.CountAsync(b => b.Status == "Approved" || b.Status == "Rejected");
            var totalRechazados = await _pinContext.Businesses.CountAsync(b => b.Status == "Rejected");
            double tasaCalculada = totalResueltos > 0 ? ((double)totalRechazados / totalResueltos) * 100 : 0;
            var nuevasSolicitudesHoy = await _pinContext.Businesses.CountAsync(b => b.Status == "Pending" && b.CreatedAt.Date == DateTime.UtcNow.Date);
            ViewBag.NuevasSolicitudesHoy = nuevasSolicitudesHoy;

            var model = new ModeracionViewModel
            {
                SolicitudesPendientes = await _pinContext.Businesses.CountAsync(b => b.Status == "Pending"),
                NegociosAprobadosHoy = await _pinContext.Businesses.CountAsync(b => b.Status == "Approved" && b.CreatedAt.Date == DateTime.UtcNow.Date),
                TasaRechazo = tasaCalculada,
                DenunciasPendientes = await _pinContext.BusinessReports.Include(r => r.Business).ThenInclude(b => b.Category).Include(r => r.Business).ThenInclude(b => b.Images).Where(r => r.ReportStatus == "Open").ToListAsync(),
                ActividadReciente = await _pinContext.StaffLogs.Include(l => l.Staff).OrderByDescending(l => l.ExecutedAt).Take(6).ToListAsync()
            };
            
            ViewBag.NegociosPendientes = await _pinContext.Businesses.Include(b => b.Category).Where(b => b.Status == "Pending").ToListAsync();
            return View(model);
        }

        public async Task<IActionResult> Perfil()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            
            var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == usuario);
            if (pinUser != null) HttpContext.Session.SetString("IsPremium", pinUser.IsPremium ? "True" : "False");
            
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
                model.Favorites = await _pinContext.Favorites.Include(f => f.Business).ThenInclude(b => b.Category).Include(f => f.Business).ThenInclude(b => b.Images).Where(f => f.UserId == user.Id).ToListAsync();
            }
            return View("~/Views/Home/Perfil/Index.cshtml", model);
        }

        public async Task<IActionResult> MisResenas()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            
            var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == usuario);
            if (pinUser != null) HttpContext.Session.SetString("IsPremium", pinUser.IsPremium ? "True" : "False");
            
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Reviews = await _pinContext.Reviews.Include(r => r.Business).Where(r => r.UserId == user.Id).ToListAsync();
            }
            return View("~/Views/Home/Perfil/MisResenas.cshtml", model);
        }

        public async Task<IActionResult> AjustesCuenta()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            
            var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == usuario);
            if (pinUser != null) HttpContext.Session.SetString("IsPremium", pinUser.IsPremium ? "True" : "False");
            
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
            }
            return View("~/Views/Home/Perfil/AjustesCuenta.cshtml", model);
        }

        [HttpPost] public async Task<IActionResult> ActualizarPerfil(string FullName, string Ubicacion, string Bio)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null) { user.Nombre = FullName; user.Ubicacion = Ubicacion; user.Bio = Bio; await _context.SaveChangesAsync(); HttpContext.Session.SetString("Nombre", FullName); }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost] public async Task<IActionResult> ActualizarFoto(IFormFile fotoPerfil)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            if (fotoPerfil != null && fotoPerfil.Length > 0)
            {
                var url = await _photoService.SubirImagenAsync(fotoPerfil);
                if (!string.IsNullOrEmpty(url))
                {
                    var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                    if (user != null) { user.FotoUrl = url; await _context.SaveChangesAsync(); HttpContext.Session.SetString("Foto", user.FotoUrl); }
                    
                    var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                    if (pinUser != null) { pinUser.ProfilePic = url; await _pinContext.SaveChangesAsync(); }
                }
            }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost] public async Task<IActionResult> EliminarFoto()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null) { user.FotoUrl = null; await _context.SaveChangesAsync(); HttpContext.Session.Remove("Foto"); }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost] public async Task<IActionResult> CambiarPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            if (NewPassword != ConfirmPassword) return RedirectToAction("AjustesCuenta");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null && user.Password == CurrentPassword) { user.Password = NewPassword; await _context.SaveChangesAsync(); }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost] public async Task<IActionResult> EliminarCuenta()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null) { _context.Usuarios.Remove(user); await _context.SaveChangesAsync(); HttpContext.Session.Clear(); return RedirectToAction("Index", "Home"); }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> AgregarResena(int BusinessId, int Rating, string Comment)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Index", "Login");
            var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (pinUser == null)
            {
                var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                if (localUser == null) return RedirectToAction("Index", "Login");
                pinUser = new User { Email = email, FullName = localUser.Nombre ?? "Usuario", PasswordHash = localUser.Password ?? "", ProfilePic = "", RoleId = 1 };
                _pinContext.Users.Add(pinUser); await _pinContext.SaveChangesAsync();
            }
            _pinContext.Reviews.Add(new Review { BusinessId = BusinessId, UserId = pinUser.UserId, Rating = Rating, Comment = Comment, CreatedAt = DateTime.UtcNow });
            await _pinContext.SaveChangesAsync();
            return RedirectToAction("InfNegocio", new { id = BusinessId });
        }

        [HttpPost] public async Task<IActionResult> EliminarResena(int reviewId)
        {
            var review = await _pinContext.Reviews.FindAsync(reviewId);
            if (review != null) { _pinContext.Reviews.Remove(review); await _pinContext.SaveChangesAsync(); }
            return RedirectToAction("MisResenas");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoNegocio(int businessId, string nuevoEstado, string returnUrl = null)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return Unauthorized("Solo los moderadores pueden realizar esta acción.");
            var negocio = await _pinContext.Businesses.FindAsync(businessId);
            if (negocio != null)
            {
                negocio.Status = nuevoEstado;
                if (nuevoEstado == "Approved")
                {
                    var pinUser = await _pinContext.Users.FindAsync(negocio.OwnerId);
                    if (pinUser != null)
                    {
                        var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == pinUser.Email);
                        if (localUser != null && localUser.Rol == "CLIENTE") localUser.Rol = "DUEÑO";
                    }
                }
                var staffEmail = HttpContext.Session.GetString("Usuario");
                var staff = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == staffEmail);
                if (staff != null) { _pinContext.Add(new StaffLog { StaffId = staff.UserId, Action = nuevoEstado == "Approved" ? $"Aprobó el negocio '{negocio.TradeName}'" : $"Rechazó/Suspendió el negocio '{negocio.TradeName}'", ExecutedAt = DateTime.UtcNow }); }
                await _pinContext.SaveChangesAsync(); await _context.SaveChangesAsync();
            }
            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Moderacion");
        }

        [HttpGet]
        public async Task<IActionResult> EditarNegocioModerador(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            var negocio = await _pinContext.Businesses
                .Include(b => b.Schedules)
                .FirstOrDefaultAsync(b => b.BusinessId == id);
            if (negocio == null) return NotFound();

            ViewBag.Categorias = await _pinContext.Categories.ToListAsync();
            return View(negocio);
        }

        [HttpPost]
        public async Task<IActionResult> EditarNegocioModerador(int BusinessId, string TradeName, string Description, string Address, string ContactPhone, int CategoryId, decimal Latitude, decimal Longitude, IFormFile NuevaImagen, string NuevaImagenUrl, string RUC, string lv_inicio, string lv_fin, string s_inicio, string s_fin, string d_inicio, string d_fin, string domingo)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return Unauthorized();

            var negocio = await _pinContext.Businesses.Include(b => b.Schedules).FirstOrDefaultAsync(b => b.BusinessId == BusinessId);
            if (negocio != null)
            {
                negocio.TradeName = TradeName;
                negocio.Description = Description;
                negocio.Address = Address;
                negocio.ContactPhone = ContactPhone;
                negocio.CategoryId = CategoryId;
                negocio.Latitude = Latitude;
                negocio.Longitude = Longitude;
                negocio.RUC = RUC ?? "";

                // Actualizar Horarios
                var oldSchedules = await _pinContext.BusinessSchedules.Where(s => s.BusinessId == BusinessId).ToListAsync();
                _pinContext.BusinessSchedules.RemoveRange(oldSchedules);

                if (!string.IsNullOrEmpty(lv_inicio) && !string.IsNullOrEmpty(lv_fin))
                {
                    _pinContext.BusinessSchedules.Add(new BusinessSchedule { BusinessId = BusinessId, DayOfWeek = "Lunes-Viernes", OpenTime = TimeSpan.Parse(lv_inicio), CloseTime = TimeSpan.Parse(lv_fin) });
                }
                if (!string.IsNullOrEmpty(s_inicio) && !string.IsNullOrEmpty(s_fin))
                {
                    _pinContext.BusinessSchedules.Add(new BusinessSchedule { BusinessId = BusinessId, DayOfWeek = "Sábados", OpenTime = TimeSpan.Parse(s_inicio), CloseTime = TimeSpan.Parse(s_fin) });
                }
                if (domingo == "abierto" && !string.IsNullOrEmpty(d_inicio) && !string.IsNullOrEmpty(d_fin))
                {
                    _pinContext.BusinessSchedules.Add(new BusinessSchedule { BusinessId = BusinessId, DayOfWeek = "Domingos", OpenTime = TimeSpan.Parse(d_inicio), CloseTime = TimeSpan.Parse(d_fin) });
                }

                // Handle Photo Update
                string newImageUrl = null;
                if (!string.IsNullOrEmpty(NuevaImagenUrl))
                {
                    newImageUrl = await _photoService.SubirImagenPorUrlAsync(NuevaImagenUrl);
                }
                else if (NuevaImagen != null && NuevaImagen.Length > 0)
                {
                    newImageUrl = await _photoService.SubirImagenAsync(NuevaImagen);
                }

                if (!string.IsNullOrEmpty(newImageUrl))
                {
                    // Remove existing images
                    var existingImages = _pinContext.BusinessImages.Where(bi => bi.BusinessId == BusinessId);
                    _pinContext.BusinessImages.RemoveRange(existingImages);
                    // Add new image
                    _pinContext.BusinessImages.Add(new BusinessImage { BusinessId = BusinessId, ImageUrl = newImageUrl });
                }

                var staffEmail = HttpContext.Session.GetString("Usuario");
                var staff = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == staffEmail);
                if (staff != null) 
                { 
                    _pinContext.Add(new StaffLog { StaffId = staff.UserId, Action = $"Editó información del negocio '{TradeName}'", ExecutedAt = DateTime.UtcNow }); 
                }
                
                await _pinContext.SaveChangesAsync();
                TempData["ExitoEdicion"] = $"El negocio '{TradeName}' fue editado correctamente.";
            }
            return RedirectToAction("NegociosAdmin");
        }

        public async Task<IActionResult> CategoriasAdmin(string busqueda, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            var query = _pinContext.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(busqueda)) query = query.Where(c => c.Name.ToLower().Contains(busqueda.ToLower()));
            int pageSize = 10;
            var totalCategorias = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalCategorias / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;
            ViewBag.Busqueda = busqueda; ViewBag.PaginaActual = pagina; ViewBag.TotalPaginas = totalPaginas; ViewBag.TotalCategorias = totalCategorias;
            var categorias = await query.OrderBy(c => c.Name).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(categorias);
        }

        [HttpPost] public async Task<IActionResult> CrearCategoria(string Name)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR" && !string.IsNullOrEmpty(Name)) { _pinContext.Categories.Add(new Category { Name = Name }); await _pinContext.SaveChangesAsync(); }
            return RedirectToAction("CategoriasAdmin");
        }

        [HttpPost] public async Task<IActionResult> EditarCategoria(int CategoryId, string Name)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR" && !string.IsNullOrEmpty(Name))
            {
                var categoria = await _pinContext.Categories.FindAsync(CategoryId);
                if (categoria != null) { categoria.Name = Name; await _pinContext.SaveChangesAsync(); }
            }
            return RedirectToAction("CategoriasAdmin");
        }

        [HttpPost] public async Task<IActionResult> EliminarCategoria(int CategoryId)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR")
            {
                var categoria = await _pinContext.Categories.FindAsync(CategoryId);
                if (categoria != null) { _pinContext.Categories.Remove(categoria); await _pinContext.SaveChangesAsync(); }
            }
            return RedirectToAction("CategoriasAdmin");
        }

        public async Task<IActionResult> NegociosAdmin(string busqueda, string filtroEstado, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            var query = _pinContext.Businesses.Include(b => b.Category).AsQueryable();
            if (!string.IsNullOrEmpty(busqueda)) query = query.Where(b => b.TradeName.ToLower().Contains(busqueda.ToLower()) || b.Address.ToLower().Contains(busqueda.ToLower()));
            if (!string.IsNullOrEmpty(filtroEstado) && filtroEstado != "TODOS") query = query.Where(b => b.Status == filtroEstado);
            int pageSize = 10;
            var totalNegocios = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalNegocios / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;
            ViewBag.Busqueda = busqueda; ViewBag.FiltroEstado = filtroEstado; ViewBag.PaginaActual = pagina; ViewBag.TotalPaginas = totalPaginas; ViewBag.TotalNegocios = totalNegocios;
            var negocios = await query.OrderByDescending(b => b.CreatedAt).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(negocios);
        }

        public async Task<IActionResult> UsuariosAdmin(string busqueda, string filtroRol, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            var query = _context.Usuarios.AsQueryable();
            if (!string.IsNullOrEmpty(busqueda)) query = query.Where(u => u.Nombre.ToLower().Contains(busqueda.ToLower()) || u.Correo.ToLower().Contains(busqueda.ToLower()));
            if (!string.IsNullOrEmpty(filtroRol) && filtroRol != "TODOS") query = query.Where(u => u.Rol == filtroRol);
            int pageSize = 10;
            var totalUsuarios = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalUsuarios / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;
            ViewBag.Busqueda = busqueda; ViewBag.FiltroRol = filtroRol; ViewBag.PaginaActual = pagina; ViewBag.TotalPaginas = totalPaginas; ViewBag.TotalUsuarios = totalUsuarios;
            var usuarios = await query.OrderBy(u => u.Nombre).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(usuarios);
        }

        [HttpPost] public async Task<IActionResult> CambiarRolUsuario(int usuarioId, string nuevoRol)
        {
            var rolActual = HttpContext.Session.GetString("Rol");
            if (rolActual != "MODERADOR") return Unauthorized("No tienes permisos.");
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                if (usuario.Correo == HttpContext.Session.GetString("Usuario")) { TempData["Error"] = "No puedes cambiar tu propio rol desde aquí."; return RedirectToAction("UsuariosAdmin"); }
                usuario.Rol = nuevoRol; await _context.SaveChangesAsync();
            }
            return RedirectToAction("UsuariosAdmin");
        }

        public async Task<IActionResult> ReportesAdmin(string busqueda, string filtroEstado, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            var query = _pinContext.BusinessReports.Include(r => r.Business).ThenInclude(b => b.Images).Include(r => r.Business).ThenInclude(b => b.Category).AsQueryable();
            if (!string.IsNullOrEmpty(busqueda)) query = query.Where(r => r.Business.TradeName.ToLower().Contains(busqueda.ToLower()));
            if (!string.IsNullOrEmpty(filtroEstado) && filtroEstado != "TODOS") query = query.Where(r => r.ReportStatus == filtroEstado);
            int pageSize = 10;
            var totalReportes = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalReportes / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;
            ViewBag.Busqueda = busqueda; ViewBag.FiltroEstado = filtroEstado; ViewBag.PaginaActual = pagina; ViewBag.TotalPaginas = totalPaginas; ViewBag.TotalReportes = totalReportes;
            var reportes = await query.OrderByDescending(r => r.CreatedAt).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(reportes);
        }

        [HttpPost] public async Task<IActionResult> ResolverReporte(int reportId, string accion, string returnUrl = null)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return Unauthorized("No tienes permisos.");
            var reporte = await _pinContext.BusinessReports.Include(r => r.Business).FirstOrDefaultAsync(r => r.ReportId == reportId);
            if (reporte != null && reporte.ReportStatus == "Open")
            {
                if (accion == "DESCARTAR") reporte.ReportStatus = "Closed";
                else if (accion == "SANCIONAR") { reporte.ReportStatus = "Resolved"; if (reporte.Business != null) reporte.Business.Status = "Rejected"; }
                var staffEmail = HttpContext.Session.GetString("Usuario");
                var staff = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == staffEmail);
                if (staff != null) { _pinContext.Add(new StaffLog { StaffId = staff.UserId, Action = accion == "DESCARTAR" ? $"Descartó la denuncia del negocio '{reporte.Business?.TradeName}'" : $"Sancionó el negocio '{reporte.Business?.TradeName}' por denuncia", ExecutedAt = DateTime.UtcNow }); }
                await _pinContext.SaveChangesAsync();
            }
            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("ReportesAdmin");
        }

        // ==========================================
        // IMPORTACIÓN DE NEGOCIOS DESDE OVERPASS API
        // ==========================================
        [HttpGet]
        public IActionResult ImportarOSM()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ImportarOSM(string latitud, string longitud, int radio = 1000)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return Unauthorized("No tienes permisos.");

            // Parsear valores con formato invariante
            if (!double.TryParse(latitud, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double latitudParsed))
            {
                TempData["Error"] = $"Latitud inválida: '{latitud}'. Debe ser un número decimal válido.";
                return RedirectToAction("ImportarOSM");
            }
            if (!double.TryParse(longitud, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double longitudParsed))
            {
                TempData["Error"] = $"Longitud inválida: '{longitud}'. Debe ser un número decimal válido.";
                return RedirectToAction("ImportarOSM");
            }

            // Validar valores
            if (latitudParsed < -90 || latitudParsed > 90)
            {
                TempData["Error"] = $"Latitud inválida: {latitudParsed}. Debe estar entre -90 y 90.";
                return RedirectToAction("ImportarOSM");
            }
            if (longitudParsed < -180 || longitudParsed > 180)
            {
                TempData["Error"] = $"Longitud inválida: {longitudParsed}. Debe estar entre -180 y 180.";
                return RedirectToAction("ImportarOSM");
            }
            if (radio < 100 || radio > 5000)
            {
                TempData["Error"] = $"Radio inválido: {radio}. Debe estar entre 100 y 5000 metros.";
                return RedirectToAction("ImportarOSM");
            }

            try
            {
                var cantidad = await _overpassService.ImportarNegociosCercanos(latitudParsed, longitudParsed, radio);
                TempData["Exito"] = $"¡Importación completada! Se agregaron {cantidad} negocios nuevos a la base de datos.";
            }
            catch (Exception ex) { TempData["Error"] = $"Error al importar: {ex.Message}"; }
            return RedirectToAction("ImportarOSM");
        }

        public async Task<IActionResult> GenerarDatosDePrueba()
        {
// ... existing code ...
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
            
            var b1 = await _pinContext.Businesses.FirstOrDefaultAsync(b => b.TradeName == "Cevichería Punto Azul");
            var b2 = await _pinContext.Businesses.FirstOrDefaultAsync(b => b.TradeName == "TechCenter Lima");
            var b3 = await _pinContext.Businesses.FirstOrDefaultAsync(b => b.TradeName == "Taller FastFix");

            if (b1 == null)
            {
                b1 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catRestaurantes!.CategoryId, TradeName = "Cevichería Punto Azul", Description = "Los mejores pescados y mariscos frescos del día.", Address = "Calle San Martín 595, Miraflores", Latitude = (decimal)-12.1245, Longitude = (decimal)-77.0250, ContactPhone = "987654321", Status = "Promoted", CreatedAt = DateTime.UtcNow, RUC = "20000000001" };
                b2 = new Business { OwnerId = pinUserCarlos!.UserId, CategoryId = catTecnologia!.CategoryId, TradeName = "TechCenter Lima", Description = "Venta de laptops y accesorios gamer.", Address = "Av. Arenales 1234, San Isidro", Latitude = (decimal)-12.0833, Longitude = (decimal)-77.0355, ContactPhone = "999888777", Status = "Approved", CreatedAt = DateTime.UtcNow, RUC = "20000000002" };
                b3 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catServicios!.CategoryId, TradeName = "Taller FastFix", Description = "Mantenimiento y pintura automotriz.", Address = "Av. Santiago de Surco 456, Surco", Latitude = (decimal)-12.1388, Longitude = (decimal)-76.9989, ContactPhone = "912345678", Status = "Approved", CreatedAt = DateTime.UtcNow, RUC = "20000000003" };
                _pinContext.Businesses.AddRange(b1, b2, b3);
                await _pinContext.SaveChangesAsync();
            }
            
            // --- SEEDING DE USUARIO PREMIUM Y SU NEGOCIO ---
            if (!await _context.Usuarios.AnyAsync(u => u.Correo == "premium@gmail.com"))
            {
                _context.Usuarios.Add(new Usuario { Nombre = "Dueño Premium", Correo = "premium@gmail.com", Password = "123", Rol = "DUEÑO", TipoAuth = "NORMAL", FotoUrl = "https://ui-avatars.com/api/?name=Dueno+Premium&background=ff6b00&color=fff", IsPremium = true });
                await _context.SaveChangesAsync();
            }

            if (!await _pinContext.Users.AnyAsync(u => u.Email == "premium@gmail.com"))
            {
                var rolDueno = await _pinContext.Roles.FirstOrDefaultAsync(r => r.Name == "DUEÑO") ?? rol;
                _pinContext.Users.Add(new User { Email = "premium@gmail.com", FullName = "Dueño Premium", PasswordHash = "123", RoleId = rolDueno!.RoleId, IsPremium = true, ProfilePic = "https://ui-avatars.com/api/?name=Dueno+Premium&background=ff6b00&color=fff" });
                await _pinContext.SaveChangesAsync();
            }

            var pinUserPremium = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == "premium@gmail.com");
            var catPremium = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Salud y Belleza");
            var bPremium = await _pinContext.Businesses.FirstOrDefaultAsync(b => b.TradeName == "Premium Spa & Wellness");

            if (bPremium == null && pinUserPremium != null && catPremium != null)
            {
                bPremium = new Business { OwnerId = pinUserPremium.UserId, CategoryId = catPremium.CategoryId, TradeName = "Premium Spa & Wellness", Description = "Spa exclusivo con beneficios para clientes VIP.", Address = "Av. Primavera 123, Surco", Latitude = (decimal)-12.1023, Longitude = (decimal)-76.9845, ContactPhone = "999000111", Status = "Promoted", CreatedAt = DateTime.UtcNow, RUC = "20000000004" };
                _pinContext.Businesses.Add(bPremium);
                await _pinContext.SaveChangesAsync();
            }

            // Generar algunas interacciones falsas para que su Dashboard tenga datos
            if (bPremium != null && !await _pinContext.BusquedasUsuario.AnyAsync(b => b.NegocioId == bPremium.BusinessId))
            {
                var random = new Random();
                var tipos = new[] { 0, 0, 0, 0, 0, 1, 1, 1, 2 }; // Más vistas(0), algunos clics(1), pocos favoritos(2)
                for (int i = 0; i < 60; i++)
                {
                    _pinContext.BusquedasUsuario.Add(new BusquedaUsuario 
                    { 
                        UsuarioId = pinUserPremium.UserId, 
                        NegocioId = bPremium.BusinessId, 
                        Categoria = catPremium.Name, 
                        Zona = "Surco", 
                        FechaBusqueda = DateTime.UtcNow.AddDays(-random.Next(0, 30)), 
                        TipoInteraccion = tipos[random.Next(tipos.Length)]
                    });
                }
                await _pinContext.SaveChangesAsync();
            }

            // NO agregar imágenes ficticias - los dueños subirán sus propias imágenes reales
            
            // Asegurar que tengan reseñas
            if (!await _pinContext.Reviews.AnyAsync(r => r.BusinessId == b1.BusinessId))
            {
                _pinContext.Reviews.AddRange(new Review { BusinessId = b1.BusinessId, UserId = pinUserCarlos!.UserId, Rating = 5, Comment = "¡Excelente!", CreatedAt = DateTime.UtcNow.AddDays(-2) }, new Review { BusinessId = b1.BusinessId, UserId = pinUserLucia!.UserId, Rating = 4, Comment = "Muy bueno", CreatedAt = DateTime.UtcNow.AddDays(-1) });
            }
            if (!await _pinContext.Reviews.AnyAsync(r => r.BusinessId == b2.BusinessId))
            {
                _pinContext.Reviews.Add(new Review { BusinessId = b2.BusinessId, UserId = pinUserLucia!.UserId, Rating = 5, Comment = "Buen servicio", CreatedAt = DateTime.UtcNow });
            }

            await _pinContext.SaveChangesAsync();
            return Content("¡ÉXITO! Base de datos poblada con imágenes y reseñas aseguradas.");
        }

    }
}