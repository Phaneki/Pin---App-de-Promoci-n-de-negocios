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
    }
}