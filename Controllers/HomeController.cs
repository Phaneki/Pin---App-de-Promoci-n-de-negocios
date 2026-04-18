using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Models;

namespace PinAppdePromo.Controllers;

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
                ImagenUrl = "~/images/picanteria.jpg",
                Descripcion = "Especialistas en comida criolla peruana con los mejores ceviches y anticuchos.",
                Horarios = "Lunes a Domingo: 12:00 PM - 11:00 PM",
                Contacto = "+51 987 654 321",
                SitioWeb = "www.lapicanteriadelSur.com"
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
                ImagenUrl = "https://via.placeholder.com/300x200",
                Descripcion = "Tienda de conveniencia con productos de primera necesidad a precios accesibles.",
                Horarios = "Lunes a Sábado: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 123 456",
                SitioWeb = "www.masporMenos.com"
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
                ImagenUrl = "https://via.placeholder.com/300x200",
                Descripcion = "Supermercado moderno con entrega a domicilio y productos frescos.",
                Horarios = "Lunes a Domingo: 7:00 AM - 11:00 PM",
                Contacto = "+51 987 789 012",
                SitioWeb = "www.autoserviciorapido.com"
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
                ImagenUrl = "~/images/cafe.jpg",
                Descripcion = "Café acogedor con especialidades en café orgánico y pasteles artesanales.",
                Horarios = "Lunes a Viernes: 7:00 AM - 9:00 PM, Sábado y Domingo: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 345 678",
                SitioWeb = "www.cafedelaesquina.com"
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
                ImagenUrl = "https://via.placeholder.com/300x200",
                Descripcion = "Servicios de barbería profesional con cortes modernos y tratamientos capilares.",
                Horarios = "Martes a Domingo: 9:00 AM - 8:00 PM",
                Contacto = "+51 987 567 890",
                SitioWeb = "www.barberiaelite.com"
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
                ImagenUrl = "~/images/picanteria.jpg",
                Descripcion = "Especialistas en comida criolla peruana con los mejores ceviches y anticuchos.",
                Horarios = "Lunes a Domingo: 12:00 PM - 11:00 PM",
                Contacto = "+51 987 654 321",
                SitioWeb = "www.lapicanteriadelSur.com"
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
                ImagenUrl = "",
                Descripcion = "Tienda de conveniencia con productos de primera necesidad a precios accesibles.",
                Horarios = "Lunes a Sábado: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 123 456",
                SitioWeb = "www.masporMenos.com"
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
                ImagenUrl = null,
                Descripcion = "Supermercado moderno con entrega a domicilio y productos frescos.",
                Horarios = "Lunes a Domingo: 7:00 AM - 11:00 PM",
                Contacto = "+51 987 789 012",
                SitioWeb = "www.autoserviciorapido.com"
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
                ImagenUrl = "~/images/cafe.jpg",
                Descripcion = "Café acogedor con especialidades en café orgánico y pasteles artesanales.",
                Horarios = "Lunes a Viernes: 7:00 AM - 9:00 PM, Sábado y Domingo: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 345 678",
                SitioWeb = "www.cafedelaesquina.com"
            }
        };

        return View(negocios);
    }

    public IActionResult InfNegocio(int id)
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
                ImagenUrl = "~/images/picanteria.jpg",
                Descripcion = "Especialistas en comida criolla peruana con los mejores ceviches y anticuchos.",
                Horarios = "Lunes a Domingo: 12:00 PM - 11:00 PM",
                Contacto = "+51 987 654 321",
                SitioWeb = "www.lapicanteriadelSur.com"
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
                ImagenUrl = "",
                Descripcion = "Tienda de conveniencia con productos de primera necesidad a precios accesibles.",
                Horarios = "Lunes a Sábado: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 123 456",
                SitioWeb = "www.masporMenos.com"
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
                ImagenUrl = null,
                Descripcion = "Supermercado moderno con entrega a domicilio y productos frescos.",
                Horarios = "Lunes a Domingo: 7:00 AM - 11:00 PM",
                Contacto = "+51 987 789 012",
                SitioWeb = "www.autoserviciorapido.com"
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
                ImagenUrl = "~/images/cafe.jpg",
                Descripcion = "Café acogedor con especialidades en café orgánico y pasteles artesanales.",
                Horarios = "Lunes a Viernes: 7:00 AM - 9:00 PM, Sábado y Domingo: 8:00 AM - 10:00 PM",
                Contacto = "+51 987 345 678",
                SitioWeb = "www.cafedelaesquina.com"
            }
        };

        var negocio = negocios.FirstOrDefault(n => n.Id == id);
        if (negocio == null)
        {
            return NotFound();
        }

        return View(negocio);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
