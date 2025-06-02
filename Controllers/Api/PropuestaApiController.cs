using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/propuestas")]
    public class PropuestaApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PropuestaApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> getPropuestas(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? usuarioId = null,
            [FromQuery] int? vacanteId = null,
            [FromQuery] string? search = null)
        {
            var baseQuery = _context.Propuestas
                .AsNoTracking()
                .AsQueryable();

            if (vacanteId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.VacanteId == vacanteId.Value);
            }
            else if (usuarioId.HasValue)
            {
                baseQuery = baseQuery.Where(p => p.UsuarioId == usuarioId);
            }



            if (!string.IsNullOrEmpty(search))
                baseQuery = baseQuery.Where(p => p.Mensaje.Contains(search));

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderBy(p => p.Id)
                .Skip((page - 1) * size)
                .Take(size)
                // Proyección a DTO anónimo:
                .Select(p => new
                {
                    p.Id,
                    p.Mensaje,
                    p.Monto,
                    p.FechaEnvio,
                    p.IsAceptada,
                    p.IsRechazada,
                    Vacante = new
                    {
                        p.Vacante.Id,
                        p.Vacante.Titulo,
                        p.Vacante.Descripcion,
                        // sólo los campos de Usuario creador que necesitas:
                        Usuario = new
                        {
                            p.Vacante.Usuario.Id,
                            p.Vacante.Usuario.Nombre,
                            p.Vacante.Usuario.Apellido
                        }
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                Size = size,
                Items = items
            });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Propuesta>> GetPropuestaById(int id)
        {
            var propuesta = await _context.Propuestas
                .Include(p => p.Usuario)
                .Include(p => p.Vacante)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (propuesta == null)
                return NotFound("Propuesta no encontrada");

            return Ok(propuesta);
        }


        [HttpPost]
        public async Task<ActionResult<Propuesta>> CreatePropuesta([FromBody] Propuesta propuesta)
        {
            if (propuesta == null)
                return BadRequest("Propuesta no puede ser nula");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Propuestas.Add(propuesta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPropuestaById), new { id = propuesta.Id }, propuesta);
        }





    }
}