using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TPFinalAvgustin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/pagos")]

    [Authorize]
    public class PagoApiController : Controller
    {
        private readonly AppDbContext _context;

        public PagoApiController(AppDbContext context)
        {
            _context = context;
        }
        // GET /pagoapi
        [HttpGet("get-pagos")]
        public async Task<IActionResult> GetPagos(
             [FromQuery] int page = 1,
             [FromQuery] int size = 10,
             [FromQuery] bool? mine = false,
             [FromQuery] string? estado = null,
             [FromQuery] DateTime? fechaDesde = null,
             [FromQuery] DateTime? fechaHasta = null)
        {
            try
            {
                var query = _context.Pagos.AsQueryable();
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                Console.WriteLine("ID DEL USUARIO : ", usuarioId);
                if (mine == true)
                    query = query.Where(p => p.ContratadoId == usuarioId);

                if (fechaDesde.HasValue)
                    query = query.Where(p => p.FechaCreacion >= fechaDesde.Value);

                if (fechaHasta.HasValue)
                    query = query.Where(p => p.FechaCreacion <= fechaHasta.Value);
                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(p => p.Estado == estado);


                var total = await query.CountAsync();

                Console.WriteLine("HASTA ACA LLEGAMOS 2 ");

                var items = await query
                    .OrderBy(c => c.Id)
                    .Skip((page - 1) * size)
                    .Take(size)
                    .Select(c => new
                    {
                        c.Id,
                        c.ContratoId,
                        c.ContratanteId,
                        c.ContratadoId,
                        c.Monto,
                        c.Estado,
                        c.FechaPago,
                        c.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total,
                    page,
                    size,
                    items
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener los pagos: " + ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }


        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}