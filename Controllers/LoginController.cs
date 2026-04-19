using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace PinAppdePromo.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string correo, string password)
        {
            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.Correo == correo && u.Password == password);

            if (usuario != null)
            {
                HttpContext.Session.SetString("Usuario", usuario.Correo);
                HttpContext.Session.SetString("Rol", usuario.Rol);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync("Cookies");

            if (result?.Principal != null)
            {
                var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                // 🔍 Verificar si existe en BD
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == email);

                if (usuario == null)
                {
                    // 👉 crear automáticamente
                    usuario = new Usuario
                    {
                        Correo = email!,
                        Password = "",
                        Rol = "CLIENTE"
                    };

                    _context.Usuarios.Add(usuario);
                    _context.SaveChanges();
                }

                // 👉 guardar sesión
                HttpContext.Session.SetString("Usuario", usuario.Correo!);
                HttpContext.Session.SetString("Rol", usuario.Rol);

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // 🔥 borra TODO
            return RedirectToAction("Index", "Home");
        }
    }
}