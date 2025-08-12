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
    [Route("propuesta")]
    [Authorize]
    public class PropuestaController : Controller

    {
        // GET /Lista
        [HttpGet("")]
        public IActionResult Index()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var model = new Usuario
            {
                Id = usuarioId != null ? int.Parse(usuarioId) : 0,
            };
            return View(model);
        }

        // GET /Lista/Error
        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

}