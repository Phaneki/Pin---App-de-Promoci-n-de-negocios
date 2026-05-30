using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using System.Dynamic;

namespace PinAppdePromo.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;

        public ProfileController(AppDbContext context, PinDbContext pinContext)
        {
            _context = context;
            _pinContext = pinContext;
        }

        public async Task<IActionResult> Index()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
                model.Favorites = await _pinContext.Favorites
                    .Include(f => f.Business).ThenInclude(b => b.Category)
                    .Include(f => f.Business).ThenInclude(b => b.Images)
                    .Where(f => f.UserId == user.Id)
                    .ToListAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> MisResenas()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Reviews = await _pinContext.Reviews.Include(r => r.Business).Where(r => r.UserId == user.Id).ToListAsync();
            }
            return View(model);
        }

        public async Task<IActionResult> AjustesCuenta()
        {
            var usuario = HttpContext.Session.GetString("Usuario");
            if (usuario == null) return RedirectToAction("Index", "Login");
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == usuario);
            dynamic model = new ExpandoObject();
            if (user != null)
            {
                model.FullName = user.Nombre;
                model.CreatedAt = DateTime.UtcNow;
                model.Ubicacion = user.Ubicacion;
                model.Bio = user.Bio;
            }
            return View(model);
        }

        [HttpPost] 
        public async Task<IActionResult> ActualizarPerfil(string FullName, string Ubicacion, string Bio)
        {
            var email = HttpContext.Session.GetString("Usuario");
            if (email == null) return RedirectToAction("Index", "Login");
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null) 
            { 
                user.Nombre = FullName; 
                user.Ubicacion = Ubicacion; 
                user.Bio = Bio; 
                await _context.SaveChangesAsync(); 
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
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
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
            
            if (NewPassword != ConfirmPassword) return RedirectToAction("AjustesCuenta");
            
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);
            if (user != null && user.Password == CurrentPassword) 
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
                HttpContext.Session.Clear(); 
                return RedirectToAction("Index", "Home"); 
            }
            return RedirectToAction("AjustesCuenta");
        }
    }
}
