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
                HttpContext.Session.SetString("Nombre", usuario.Nombre ?? "");
                HttpContext.Session.SetString("Foto", usuario.FotoUrl ?? "");
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }

        [HttpPost]
        public IActionResult Registrar(string correo, string password, string confirmarPassword)
        {
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Completa todos los campos";
                return View();
            }

            if (password != confirmarPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden";
                return View();
            }

            var existe = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);

            if (existe != null)
            {
                ViewBag.Error = "El correo ya está registrado";
                return View();
            }

            var usuario = new Usuario
            {
                Correo = correo,
                Password = password,
                Rol = "CLIENTE"
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            // 🔥 LOGIN AUTOMÁTICO
            HttpContext.Session.SetString("Usuario", usuario.Correo);
            HttpContext.Session.SetString("Rol", usuario.Rol);
            HttpContext.Session.SetString("Nombre", usuario.Nombre ?? "");
            HttpContext.Session.SetString("Foto", usuario.FotoUrl ?? "");

            return RedirectToAction("Index", "Home");
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
                var nombre = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                var foto = result.Principal.FindFirst("picture")?.Value;
                
                if (string.IsNullOrEmpty(email))
                {
                    return RedirectToAction("Index");
                }
                // 🔍 Verificar si existe en BD
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == email);

                if (usuario == null)
                {
                    // 👉 crear automáticamente
                    usuario = new Usuario
                    {
                        Correo = email!,
                        Nombre = nombre,
                        FotoUrl = foto,
                        Password = "",
                        Rol = "CLIENTE"
                    };

                    _context.Usuarios.Add(usuario);
                    _context.SaveChanges();
                }
                else
                {
                    // 🔥 ACTUALIZA SI YA EXISTE
                    if (!string.IsNullOrEmpty(nombre))
                        usuario.Nombre = nombre;

                    if (!string.IsNullOrEmpty(foto))
                        usuario.FotoUrl = foto;
                }
                _context.SaveChanges();

                // 👉 guardar sesión
                HttpContext.Session.SetString("Usuario", usuario.Correo!);
                HttpContext.Session.SetString("Rol", usuario.Rol);
                HttpContext.Session.SetString("Nombre", usuario.Nombre ?? "");
                HttpContext.Session.SetString("Foto", usuario.FotoUrl ?? "");

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // 🔥 borra TODO
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Registrar()
        {
            return View();
        }
    }
}