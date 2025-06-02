using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Console.WriteLine("ID de usuario: " + usuarioId);


        var model = new Usuario
        {
            Id = usuarioId != null ? int.Parse(usuarioId) : 0,

        };

        return View(model);
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
