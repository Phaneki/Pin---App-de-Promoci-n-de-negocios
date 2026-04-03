using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pin___App_de_Promoci_n_de_negocios.Models;

namespace Pin___App_de_Promoci_n_de_negocios.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var negocios = new List<Negocio>
        {
            new Negocio
            {
                Id = 1,
                Nombre = "La Picantería del Sur",
                Categoria = "Restaurantes",
                Direccion = "Av. Larco 1230, Miraflores",
                Calificacion = 4.8,
                EstaAbierto = true,
                Destacado = true,
                ImagenUrl = "~/images/picanteria.jpg"
            },
            new Negocio
            {
                Id = 2,
                Nombre = "Tienda Más Por Menos",
                Categoria = "Tiendas",
                Direccion = "Calle Las Ciencias 55, San Isidro",
                Calificacion = 4.3,
                EstaAbierto = false,
                Destacado = false,
                ImagenUrl = "https://via.placeholder.com/300x200"
            },
            new Negocio
            {
                Id = 3,
                Nombre = "AutoServicio Rápido",
                Categoria = "Supermercados",
                Direccion = "Av. La Marina 980, Surco",
                Calificacion = 4.5,
                EstaAbierto = true,
                Destacado = false,
                ImagenUrl = "https://via.placeholder.com/300x200"
            },
            new Negocio
            {
                Id = 4,
                Nombre = "Café de la Esquina",
                Categoria = "Cafés",
                Direccion = "Calle 7 777, Miraflores",
                Calificacion = 4.9,
                EstaAbierto = true,
                Destacado = true,
                ImagenUrl = "~/images/cafe.jpg"
            },

            // 👉 AGREGA MÁS AQUÍ
            new Negocio
            {
                Id = 5,
                Nombre = "Barbería Elite",
                Categoria = "Belleza",
                Direccion = "San Borja",
                Calificacion = 4.6,
                EstaAbierto = true,
                Destacado = true,
                ImagenUrl = "https://via.placeholder.com/300x200"
            }
        };

        return View(negocios);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Explorar()
    {
        var negocios = new List<Negocio>
        {
            new Negocio
            {
                Id = 1,
                Nombre = "La Picantería del Sur",
                Categoria = "Restaurantes",
                Direccion = "Av. Larco 1230, Miraflores",
                Calificacion = 4.8,
                EstaAbierto = true,
                Destacado = true,
                ImagenUrl = "~/images/picanteria.jpg"
            },
            new Negocio
            {
                Id = 2,
                Nombre = "Tienda Más Por Menos",
                Categoria = "Tiendas",
                Direccion = "Calle Las Ciencias 55, San Isidro",
                Calificacion = 4.3,
                EstaAbierto = false,
                Destacado = false,
                ImagenUrl = ""
            },
            new Negocio
            {
                Id = 3,
                Nombre = "AutoServicio Rápido",
                Categoria = "Supermercados",
                Direccion = "Av. La Marina 980, Surco",
                Calificacion = 4.5,
                EstaAbierto = true,
                Destacado = false,
                ImagenUrl = null
            },
            new Negocio
            {
                Id = 4,
                Nombre = "Café de la Esquina",
                Categoria = "Cafés",
                Direccion = "Calle 7 777, Miraflores",
                Calificacion = 4.9,
                EstaAbierto = true,
                Destacado = true,
                ImagenUrl = "~/images/cafe.jpg"
            }
        };

        return View(negocios);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
