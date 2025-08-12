using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class PagoController : Controller
    {
        private readonly ILogger<PagoController> _logger;

        public PagoController(ILogger<PagoController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var model = new Usuario
            {
                Id = usuarioId != null ? int.Parse(usuarioId) : 0,

            };

            return View(model);
        }

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}