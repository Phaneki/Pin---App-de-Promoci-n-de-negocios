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
            {
                query = query.Where(b => b.TradeName.ToLower().Contains(busqueda.ToLower()) || b.Category.Name.ToLower().Contains(busqueda.ToLower()));
            }

            if (!string.IsNullOrEmpty(distrito))
            {
                query = query.Where(b => b.Address.ToLower().Contains(distrito.ToLower()));
            }

            if (categorias != null && categorias.Any())
            {
                query = query.Where(b => categorias.Contains(b.CategoryId));
            }

            // Ordenamiento
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

            // Extraer distritos únicos de las direcciones de negocios activos
            var todosNegocios = await _pinContext.Businesses
                .Where(b => b.Status == "Approved" || b.Status == "Promoted")
                .Select(b => b.Address)
                .ToListAsync();
            ViewBag.Distritos = todosNegocios
                .Where(a => !string.IsNullOrEmpty(a))
                .Select(a => a.Split(',').Last().Trim())
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            
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
        public async Task<IActionResult> CrearNegocio(Business negocio, List<IFormFile> Imagenes, string ImageUrlLink)
        {
            try
            {
                negocio.Status = "Pending";
                negocio.CreatedAt = DateTime.UtcNow;
                negocio.RUC = negocio.RUC ?? "";
                negocio.ContactPhone = negocio.ContactPhone ?? "";

                // Obtener el OwnerId basado en el usuario logueado en la sesión
                var email = HttpContext.Session.GetString("Usuario");
                
                // Buscar el usuario en la base de datos de Pin (inglés)
                var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                
                // Si no existe en PinDbContext, lo creamos al vuelo para que no falle la Llave Foránea
                if (pinUser == null && !string.IsNullOrEmpty(email))
                {
                    var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                    pinUser = new User
                    {
                        Email = email,
                        FullName = localUser?.Nombre ?? "Usuario",
                        PasswordHash = localUser?.Password ?? "",
                        ProfilePic = "",
                        RoleId = 1 // Rol CLIENTE por defecto
                    };
                    _pinContext.Users.Add(pinUser);
                    await _pinContext.SaveChangesAsync();
                }

                negocio.OwnerId = pinUser?.UserId ?? 1; 

                _pinContext.Businesses.Add(negocio);
                await _pinContext.SaveChangesAsync();

                // 1. Si el cliente envió un Link de imagen (Opción 1)
                if (!string.IsNullOrEmpty(ImageUrlLink))
                {
                    _pinContext.BusinessImages.Add(new BusinessImage
                    {
                        BusinessId = negocio.BusinessId,
                        ImageUrl = ImageUrlLink
                    });
                    await _pinContext.SaveChangesAsync();
                }

                // 2. Simulación del guardado de imágenes subidas (Opción 2)
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
            catch (Exception ex)
            {
                // ⚠️ TEMPORAL: Mostrar el error real en pantalla para diagnosticar
                var innerMsg = ex.InnerException?.Message ?? "Sin inner exception";
                return Content($"ERROR: {ex.Message}\n\nINNER: {innerMsg}\n\nSTACK: {ex.StackTrace}", "text/plain");
            }
        }

        public async Task<IActionResult> Moderacion()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR")
            {
                return RedirectToAction("Index", "Home");
            }

            // Calcular la tasa de rechazo dinámicamente
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

            ViewBag.NegociosPendientes = await _pinContext.Businesses
                .Include(b => b.Category)
                .Where(b => b.Status == "Pending")
                .ToListAsync();

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
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
                
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
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
            }
            return View("~/Views/Home/Perfil/AjustesCuenta.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarPerfil(string FullName, string Ubicacion, string Bio)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null)
            {
                user.Nombre = FullName; // Ahora sí actualiza el Nombre del modelo Usuario
                user.Ubicacion = Ubicacion;
                user.Bio = Bio;
                await _context.SaveChangesAsync();
                
                // Actualizamos la sesión para que el cambio de nombre se vea inmediatamente
                HttpContext.Session.SetString("Nombre", FullName);
            }
            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarFoto(IFormFile fotoPerfil)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            if (fotoPerfil != null && fotoPerfil.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + fotoPerfil.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fotoPerfil.CopyToAsync(fileStream);
                }

                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                if (user != null)
                {
                    user.FotoUrl = "/images/profiles/" + uniqueFileName;
                    await _context.SaveChangesAsync();
                    
                    HttpContext.Session.SetString("Foto", user.FotoUrl);
                }
            }

            return RedirectToAction("AjustesCuenta");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarFoto()
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");

            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null)
            {
                user.FotoUrl = null;
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Foto");
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

            // Buscar en la tabla Users de PinDbContext para respetar la relación
            var pinUser = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (pinUser == null)
            {
                var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
                if (localUser == null) return RedirectToAction("Index", "Login");
                
                pinUser = new User
                {
                    Email = email,
                    FullName = localUser.Nombre ?? "Usuario",
                    PasswordHash = localUser.Password ?? "",
                    ProfilePic = "",
                    RoleId = 1 // Rol CLIENTE por defecto
                };
                _pinContext.Users.Add(pinUser);
                await _pinContext.SaveChangesAsync();
            }

            var review = new Review
            {
                BusinessId = BusinessId,
                UserId = pinUser.UserId,
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

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoNegocio(int businessId, string nuevoEstado, string returnUrl = null)
        {
            // 🔒 Validación estricta: SOLO cuentas con rol MODERADOR pueden aceptar/rechazar negocios
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR")
            {
                return Unauthorized("Solo los moderadores pueden realizar esta acción.");
            }

            var negocio = await _pinContext.Businesses.FindAsync(businessId);
            if (negocio != null)
            {
                // Los estados esperados son "Approved" o "Rejected"
                negocio.Status = nuevoEstado;

                // 🚀 NUEVA LÓGICA: Si se aprueba, convertimos al cliente en DUEÑO
                if (nuevoEstado == "Approved")
                {
                    var pinUser = await _pinContext.Users.FindAsync(negocio.OwnerId);
                    if (pinUser != null)
                    {
                        var localUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == pinUser.Email);
                        if (localUser != null && localUser.Rol == "CLIENTE")
                        {
                            localUser.Rol = "DUEÑO";
                        }
                    }
                }

                // 📝 REGISTRAR ACTIVIDAD DEL STAFF AUTOMÁTICAMENTE
                var staffEmail = HttpContext.Session.GetString("Usuario");
                var staff = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == staffEmail);
                if (staff != null)
                {
                    _pinContext.Add(new StaffLog {
                        StaffId = staff.UserId,
                        Action = nuevoEstado == "Approved" ? $"Aprobó el negocio '{negocio.TradeName}'" : $"Rechazó/Suspendió el negocio '{negocio.TradeName}'",
                        ExecutedAt = DateTime.UtcNow
                    });
                }

                await _pinContext.SaveChangesAsync();
                await _context.SaveChangesAsync(); // Guardamos el nuevo rol en _context
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Moderacion");
        }

        // ==========================================
        // ADMINISTRACIÓN DE CATEGORÍAS
        // ==========================================
        public async Task<IActionResult> CategoriasAdmin(string busqueda, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            var query = _pinContext.Categories.AsQueryable();

            // 1. Filtrar por texto (Nombre)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(c => c.Name.ToLower().Contains(busqueda.ToLower()));
            }

            // 2. Paginación (Mostrar de 10 en 10)
            int pageSize = 10;
            var totalCategorias = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalCategorias / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

            ViewBag.Busqueda = busqueda;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalCategorias = totalCategorias;

            var categorias = await query.OrderBy(c => c.Name).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria(string Name)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR" && !string.IsNullOrEmpty(Name))
            {
                _pinContext.Categories.Add(new Category { Name = Name });
                await _pinContext.SaveChangesAsync();
            }
            return RedirectToAction("CategoriasAdmin");
        }

        [HttpPost]
        public async Task<IActionResult> EditarCategoria(int CategoryId, string Name)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR" && !string.IsNullOrEmpty(Name))
            {
                var categoria = await _pinContext.Categories.FindAsync(CategoryId);
                if (categoria != null)
                {
                    categoria.Name = Name;
                    await _pinContext.SaveChangesAsync();
                }
            }
            return RedirectToAction("CategoriasAdmin");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarCategoria(int CategoryId)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol == "MODERADOR")
            {
                var categoria = await _pinContext.Categories.FindAsync(CategoryId);
                if (categoria != null)
                {
                    _pinContext.Categories.Remove(categoria);
                    await _pinContext.SaveChangesAsync();
                }
            }
            return RedirectToAction("CategoriasAdmin");
        }

        // ==========================================
        // ADMINISTRACIÓN DE NEGOCIOS
        // ==========================================
        public async Task<IActionResult> NegociosAdmin(string busqueda, string filtroEstado, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            var query = _pinContext.Businesses.Include(b => b.Category).AsQueryable();

            // 1. Filtrar por texto (Nombre o Dirección)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(b => b.TradeName.ToLower().Contains(busqueda.ToLower()) || b.Address.ToLower().Contains(busqueda.ToLower()));
            }

            // 2. Filtrar por Estado específico
            if (!string.IsNullOrEmpty(filtroEstado) && filtroEstado != "TODOS")
            {
                query = query.Where(b => b.Status == filtroEstado);
            }

            // 3. Paginación (Mostrar de 10 en 10)
            int pageSize = 10;
            var totalNegocios = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalNegocios / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

            ViewBag.Busqueda = busqueda;
            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalNegocios = totalNegocios;

            var negocios = await query.OrderByDescending(b => b.CreatedAt).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(negocios);
        }

        // ==========================================
        // ADMINISTRACIÓN DE USUARIOS
        // ==========================================
        public async Task<IActionResult> UsuariosAdmin(string busqueda, string filtroRol, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            var query = _context.Usuarios.AsQueryable();

            // 1. Filtrar por texto (Nombre o Correo)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(u => u.Nombre.ToLower().Contains(busqueda.ToLower()) || u.Correo.ToLower().Contains(busqueda.ToLower()));
            }

            // 2. Filtrar por Rol específico
            if (!string.IsNullOrEmpty(filtroRol) && filtroRol != "TODOS")
            {
                query = query.Where(u => u.Rol == filtroRol);
            }

            // 3. Paginación (Mostrar de 10 en 10)
            int pageSize = 10;
            var totalUsuarios = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalUsuarios / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

            ViewBag.Busqueda = busqueda;
            ViewBag.FiltroRol = filtroRol;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalUsuarios = totalUsuarios;

            var usuarios = await query.OrderBy(u => u.Nombre).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarRolUsuario(int usuarioId, string nuevoRol)
        {
            var rolActual = HttpContext.Session.GetString("Rol");
            if (rolActual != "MODERADOR") return Unauthorized("No tienes permisos.");

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                // Evitar que el moderador en sesión se quite el rol a sí mismo o se suspenda accidentalmente
                if (usuario.Correo == HttpContext.Session.GetString("Usuario"))
                {
                    TempData["Error"] = "No puedes cambiar tu propio rol desde aquí.";
                    return RedirectToAction("UsuariosAdmin");
                }

                usuario.Rol = nuevoRol;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("UsuariosAdmin");
        }

        // ==========================================
        // ADMINISTRACIÓN DE REPORTES
        // ==========================================
        public async Task<IActionResult> ReportesAdmin(string busqueda, string filtroEstado, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");

            var query = _pinContext.BusinessReports
                .Include(r => r.Business).ThenInclude(b => b.Images)
                .Include(r => r.Business).ThenInclude(b => b.Category)
                .AsQueryable();

            // 1. Filtrar por nombre de negocio
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(r => r.Business.TradeName.ToLower().Contains(busqueda.ToLower()));
            }

            // 2. Filtrar por Estado específico
            if (!string.IsNullOrEmpty(filtroEstado) && filtroEstado != "TODOS")
            {
                query = query.Where(r => r.ReportStatus == filtroEstado);
            }

            // 3. Paginación
            int pageSize = 10;
            var totalReportes = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalReportes / (double)pageSize);
            if (pagina < 1) pagina = 1;
            if (pagina > totalPaginas && totalPaginas > 0) pagina = totalPaginas;

            ViewBag.Busqueda = busqueda;
            ViewBag.FiltroEstado = filtroEstado;
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;
            ViewBag.TotalReportes = totalReportes;

            var reportes = await query.OrderByDescending(r => r.CreatedAt).Skip((pagina - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(reportes);
        }

        [HttpPost]
        public async Task<IActionResult> ResolverReporte(int reportId, string accion, string returnUrl = null)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return Unauthorized("No tienes permisos.");

            var reporte = await _pinContext.BusinessReports.Include(r => r.Business).FirstOrDefaultAsync(r => r.ReportId == reportId);
            if (reporte != null && reporte.ReportStatus == "Open")
            {
                if (accion == "DESCARTAR")
                {
                    reporte.ReportStatus = "Closed"; // Se cierra la denuncia por ser inválida
                }
                else if (accion == "SANCIONAR")
                {
                    reporte.ReportStatus = "Resolved"; // Se resuelve sancionando
                    if (reporte.Business != null)
                    {
                        reporte.Business.Status = "Rejected"; // Ocultamos el negocio
                    }
                }

                // 📝 REGISTRAR ACTIVIDAD DEL STAFF AUTOMÁTICAMENTE
                var staffEmail = HttpContext.Session.GetString("Usuario");
                var staff = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == staffEmail);
                if (staff != null)
                {
                    _pinContext.Add(new StaffLog {
                        StaffId = staff.UserId,
                        Action = accion == "DESCARTAR" ? $"Descartó la denuncia del negocio '{reporte.Business?.TradeName}'" : $"Sancionó el negocio '{reporte.Business?.TradeName}' por denuncia",
                        ExecutedAt = DateTime.UtcNow
                    });
                }

                await _pinContext.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("ReportesAdmin");
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

            // 1.5. Agregar Rol y Users en PinDbContext (Para que PostgreSQL no bloquee la inyección)
            if (!await _pinContext.Roles.AnyAsync())
            {
                _pinContext.Roles.Add(new Role { Name = "CLIENTE" });
                await _pinContext.SaveChangesAsync();
            }
            
            var rol = await _pinContext.Roles.FirstOrDefaultAsync();

            if (!await _pinContext.Users.AnyAsync(u => u.Email == "lucia@gmail.com"))
            {
                _pinContext.Users.AddRange(
                    new User { Email = "lucia@gmail.com", FullName = "Lucía Méndez", PasswordHash = "123", RoleId = rol!.RoleId },
                    new User { Email = "carlos@gmail.com", FullName = "Carlos Rivera", PasswordHash = "123", RoleId = rol!.RoleId }
                );
                await _pinContext.SaveChangesAsync();
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
            var pinUserLucia = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == "lucia@gmail.com");
            var pinUserCarlos = await _pinContext.Users.FirstOrDefaultAsync(u => u.Email == "carlos@gmail.com");
            var catRestaurantes = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Restaurantes");
            var catTecnologia = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Tecnología");
            var catServicios = await _pinContext.Categories.FirstOrDefaultAsync(c => c.Name == "Servicios Automotrices");

            // 3. Agregar Negocios y Relaciones
            if (!await _pinContext.Businesses.AnyAsync(b => b.TradeName == "Cevichería Punto Azul"))
            {
                var b1 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catRestaurantes!.CategoryId, TradeName = "Cevichería Punto Azul", Description = "Los mejores pescados y mariscos frescos del día. Especialidad en ceviche carretillero y jalea mixta.", Address = "Calle San Martín 595, Miraflores", Latitude = (decimal)-12.1245, Longitude = (decimal)-77.0250, ContactPhone = "987654321", Status = "Promoted", CreatedAt = DateTime.UtcNow };
                var b2 = new Business { OwnerId = pinUserCarlos!.UserId, CategoryId = catTecnologia!.CategoryId, TradeName = "TechCenter Lima", Description = "Venta de laptops, accesorios gamer y servicio técnico especializado para computadoras.", Address = "Av. Arenales 1234, San Isidro", Latitude = (decimal)-12.0833, Longitude = (decimal)-77.0355, ContactPhone = "999888777", Status = "Approved", CreatedAt = DateTime.UtcNow };
                var b3 = new Business { OwnerId = pinUserLucia!.UserId, CategoryId = catServicios!.CategoryId, TradeName = "Taller FastFix", Description = "Mantenimiento preventivo, afinamiento, planchado y pintura automotriz.", Address = "Av. Santiago de Surco 456, Surco", Latitude = (decimal)-12.1388, Longitude = (decimal)-76.9989, ContactPhone = "912345678", Status = "Approved", CreatedAt = DateTime.UtcNow };

                _pinContext.Businesses.AddRange(b1, b2, b3);
                await _pinContext.SaveChangesAsync();

                _pinContext.BusinessImages.AddRange(
                    new BusinessImage { BusinessId = b1.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1559314809-0d155014e29e?w=800&q=80" },
                    new BusinessImage { BusinessId = b2.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1531297172869-c7d6b8b82922?w=800&q=80" },
                    new BusinessImage { BusinessId = b3.BusinessId, ImageUrl = "https://images.unsplash.com/photo-1613214149922-f1809c99b414?w=800&q=80" }
                );

                _pinContext.Reviews.AddRange(
                    new Review { BusinessId = b1.BusinessId, UserId = pinUserCarlos.UserId, Rating = 5, Comment = "¡El mejor ceviche que he probado en Miraflores! Atención rápida y porciones generosas.", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new Review { BusinessId = b1.BusinessId, UserId = pinUserLucia.UserId, Rating = 4, Comment = "Muy rico todo, aunque el local estaba un poco lleno por ser fin de semana.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Review { BusinessId = b2.BusinessId, UserId = pinUserLucia.UserId, Rating = 5, Comment = "Compré una laptop para la universidad y me asesoraron súper bien. Precios justos.", CreatedAt = DateTime.UtcNow }
                );

                await _pinContext.SaveChangesAsync();
            }

            return Content("¡ÉXITO! Tu base de datos ha sido poblada con negocios reales de Lima, fotos, reseñas y usuarios. Ya puedes regresar al Inicio y probar la plataforma.");
        }
    }
}
