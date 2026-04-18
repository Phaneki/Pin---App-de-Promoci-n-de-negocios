using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Models;
using System.Linq;

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
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales incorrectas";
            return View();
        }
    }
}