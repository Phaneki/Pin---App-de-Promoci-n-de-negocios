using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pin___App_de_Promoci_n_de_negocios.Models;

namespace Pin___App_de_Promoci_n_de_negocios.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
