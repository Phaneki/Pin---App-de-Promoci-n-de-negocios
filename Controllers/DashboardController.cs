using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace PinAppdePromo.Controllers
{
    public class DashboardController : Controller
    {
        private readonly PinDbContext _context;

        public DashboardController(PinDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Verificación de Seguridad y Plan Premium
            var userEmail = HttpContext.Session.GetString("Usuario");
            var isPremium = HttpContext.Session.GetString("IsPremium") == "True";

            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Index", "Login");
            
            // Si no es premium, lo enviamos a la página para que compre la suscripción
            if (!isPremium) return RedirectToAction("Beneficios", "Home");

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Index", "Login");

            // 2. Obtener los negocios de este usuario
            var negocios = _context.Businesses
                .Where(b => b.OwnerId == user.UserId)
                .ToList();

            var negocioIds = negocios.Select(b => b.BusinessId).ToList();

            // 3. Obtener interacciones de los últimos 30 días (Tabla que usas para el ML)
            var fechaInicio = DateTime.UtcNow.AddDays(-30);
            
            // Nota: Asegúrate de que la propiedad DbSet se llame "BusquedasUsuario" en tu DbContext
            var interacciones = _context.BusquedasUsuario
                .Where(b => negocioIds.Contains(b.NegocioId) && b.FechaBusqueda >= fechaInicio)
                .ToList();

            // 4. Calcular KPIs globales (0=Vistas, 1=Clics, 2=Favoritos)
            ViewBag.TotalVistas = interacciones.Count(i => i.TipoInteraccion == 0);
            ViewBag.TotalClics = interacciones.Count(i => i.TipoInteraccion == 1);
            ViewBag.TotalFavoritos = interacciones.Count(i => i.TipoInteraccion == 2);
            
            var clics = (double)ViewBag.TotalClics;
            var vistas = (double)ViewBag.TotalVistas;
            ViewBag.CTR = vistas > 0 ? Math.Round((clics / vistas) * 100, 2) : 0;

            // 5. Preparar datos para las gráficas (Agrupados por día)
            var datosGrafica = interacciones
                .GroupBy(i => i.FechaBusqueda.Date)
                .Select(g => new {
                    Fecha = g.Key.ToString("dd MMM"),
                    Vistas = g.Count(i => i.TipoInteraccion == 0),
                    Clics = g.Count(i => i.TipoInteraccion == 1)
                })
                .OrderBy(g => g.Fecha)
                .ToList();

            ViewBag.FechasGrafica = System.Text.Json.JsonSerializer.Serialize(datosGrafica.Select(d => d.Fecha));
            ViewBag.VistasGrafica = System.Text.Json.JsonSerializer.Serialize(datosGrafica.Select(d => d.Vistas));
            ViewBag.ClicsGrafica = System.Text.Json.JsonSerializer.Serialize(datosGrafica.Select(d => d.Clics));

            return View(negocios);
        }
    }
}