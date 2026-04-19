using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Models;

namespace PinAppdePromo.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var negocios = _context.Negocios.ToList();
            return View(negocios);
        }

        public IActionResult Explorar()
        {
            var negocios = _context.Negocios.ToList();
            return View(negocios);
        }

        public IActionResult InfNegocio(int id)
        {
            var negocio = _context.Negocios.FirstOrDefault(n => n.Id == id);

            if (negocio == null)
            {
                return NotFound();
            }

            return View(negocio);
        }

        public IActionResult RegistrarNegocio()
        {
            if (HttpContext.Session.GetString("Usuario") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            if (HttpContext.Session.GetString("Rol") != "CLIENTE_NEGOCIO")
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public IActionResult Moderacion()
        {
            if (HttpContext.Session.GetString("Usuario") != "admin@pin.com")
            {
                return RedirectToAction("Index", "Home");
            }
            return View("Moderacion");
        }
    }
}
