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
             [FromQuery] int? usuarioId = 0,
             [FromQuery] string? estado = null,
             [FromQuery] DateTime? fechaDesde = null,
             [FromQuery] DateTime? fechaHasta = null)
        {
            Console.WriteLine("ID DEL USUARIO ENVIADO EN PETICION: ", usuarioId);
            var query = _context.Pagos.AsQueryable();

            if (usuarioId > 0)
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}