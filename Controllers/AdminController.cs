using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using PinAppdePromo.Services;

namespace PinAppdePromo.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;
        private readonly OverpassService _overpassService;

        public AdminController(AppDbContext context, PinDbContext pinContext, OverpassService overpassService)
        {
            _context = context;
            _pinContext = pinContext;
            _overpassService = overpassService;
        }

        public async Task<IActionResult> Moderacion()
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            
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
                    .Include(r => r.Business).ThenInclude(b => b.Images)
                    .Where(r => r.ReportStatus == "Open").ToListAsync(),
                ActividadReciente = await _pinContext.StaffLogs
                    .Include(l => l.Staff)
                    .OrderByDescending(l => l.ExecutedAt).Take(4).ToListAsync()
            };
            
            ViewBag.NegociosPendientes = await _pinContext.Businesses
                .Include(b => b.Category)
                .Where(b => b.Status == "Pending").ToListAsync();
                
            return View(model);
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
                if (staff != null) 
                { 
                    _pinContext.Add(new StaffLog { 
                        StaffId = staff.UserId, 
                        Action = nuevoEstado == "Approved" ? $"Aprobó el negocio '{negocio.TradeName}'" : $"Rechazó/Suspendió el negocio '{negocio.TradeName}'", 
                        ExecutedAt = DateTime.UtcNow 
                    }); 
                }
                
                await _pinContext.SaveChangesAsync(); 
                await _context.SaveChangesAsync();
            }
            
            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Moderacion");
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
                if (categoria != null) { categoria.Name = Name; await _pinContext.SaveChangesAsync(); }
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
            
            ViewBag.Busqueda = busqueda; 
            ViewBag.FiltroEstado = filtroEstado; 
            ViewBag.PaginaActual = pagina; 
            ViewBag.TotalPaginas = totalPaginas; 
            ViewBag.TotalNegocios = totalNegocios;
            
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

        public async Task<IActionResult> ReportesAdmin(string busqueda, string filtroEstado, int pagina = 1)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "MODERADOR") return RedirectToAction("Index", "Home");
            
            var query = _pinContext.BusinessReports
                .Include(r => r.Business).ThenInclude(b => b.Images)
                .Include(r => r.Business).ThenInclude(b => b.Category)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(busqueda)) query = query.Where(r => r.Business.TradeName.ToLower().Contains(busqueda.ToLower()));
            if (!string.IsNullOrEmpty(filtroEstado) && filtroEstado != "TODOS") query = query.Where(r => r.ReportStatus == filtroEstado);
            
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
                if (accion == "DESCARTAR") reporte.ReportStatus = "Closed";
                else if (accion == "SANCIONAR") { reporte.ReportStatus = "Resolved"; if (reporte.Business != null) reporte.Business.Status = "Rejected"; }
                
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
    }
}
