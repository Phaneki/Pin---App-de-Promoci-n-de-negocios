using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;

namespace PinAppdePromo.Controllers
{
    public class BusinessController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PinDbContext _pinContext;

        public BusinessController(AppDbContext context, PinDbContext pinContext)
        {
            _context = context;
            _pinContext = pinContext;
        }

        public async Task<IActionResult> InfNegocio(int id)
        {
            var negocio = await _pinContext.Businesses
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Include(b => b.Reviews)
                .Include(b => b.Products)
                .FirstOrDefaultAsync(n => n.BusinessId == id);
            
            if (negocio == null) return NotFound();
            
            return View(negocio);
        }

        public async Task<IActionResult> RegistrarNegocio()
        {
            if (HttpContext.Session.GetString("Usuario") == null) 
                return RedirectToAction("Index", "Login");
                
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
                    pinUser = new User { 
                        Email = email, 
                        FullName = localUser?.Nombre ?? "Usuario", 
                        PasswordHash = localUser?.Password ?? "", 
                        ProfilePic = "", 
                        RoleId = 1 
                    };
                    _pinContext.Users.Add(pinUser);
                    await _pinContext.SaveChangesAsync();
                }
                
                negocio.OwnerId = pinUser?.UserId ?? 1;
                _pinContext.Businesses.Add(negocio);
                await _pinContext.SaveChangesAsync();
                
                if (!string.IsNullOrEmpty(ImageUrlLink))
                {
                    _pinContext.BusinessImages.Add(new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = ImageUrlLink });
                    await _pinContext.SaveChangesAsync();
                }
                
                if (Imagenes != null && Imagenes.Count > 0)
                {
                    foreach (var img in Imagenes)
                    {
                        _pinContext.BusinessImages.Add(new BusinessImage { BusinessId = negocio.BusinessId, ImageUrl = $"/images/temp_{Guid.NewGuid()}_{img.FileName}" });
                    }
                    await _pinContext.SaveChangesAsync();
                }
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? "Sin inner exception";
                return Content($"ERROR: {ex.Message}\n\nINNER: {innerMsg}\n\nSTACK: {ex.StackTrace}", "text/plain");
            }
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
                
                pinUser = new User { 
                    Email = email, 
                    FullName = localUser.Nombre ?? "Usuario", 
                    PasswordHash = localUser.Password ?? "", 
                    ProfilePic = "", 
                    RoleId = 1 
                };
                _pinContext.Users.Add(pinUser); 
                await _pinContext.SaveChangesAsync();
            }
            
            _pinContext.Reviews.Add(new Review { BusinessId = BusinessId, UserId = pinUser.UserId, Rating = Rating, Comment = Comment, CreatedAt = DateTime.UtcNow });
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
            // Mover a ProfileController u home según corresponda la ruta de MisResenas
            return RedirectToAction("MisResenas", "Profile");
        }
    }
}
