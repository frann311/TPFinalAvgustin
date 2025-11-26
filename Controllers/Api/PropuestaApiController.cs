using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/propuestas")]
    [Authorize]
    public class PropuestaApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PropuestaApiController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet("listar")]
        public async Task<ActionResult> getPropuestas(
                    [FromQuery] int page = 1,
                    [FromQuery] int size = 10,
                    [FromQuery] bool? mias = false,
                    [FromQuery] int? vacanteId = null,
                    [FromQuery] string? search = null)
        {
            try
            {
                var baseQuery = _context.Propuestas
                    .AsNoTracking()
                .Include(p => p.Vacante)
                .ThenInclude(v => v.Usuario)
                .Include(p => p.Usuario)
                .AsQueryable();

                if (vacanteId.HasValue)
                {
                    baseQuery = baseQuery.Where(p => p.VacanteId == vacanteId.Value);
                }
                else if (mias == true)
                {
                    var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                    baseQuery = baseQuery.Where(p => p.UsuarioId == usuarioId);
                }



                if (!string.IsNullOrEmpty(search))
                    baseQuery = baseQuery.Where(p => p.Mensaje.Contains(search));

                var total = await baseQuery.CountAsync();

                var items = await baseQuery
                    .OrderByDescending(p => p.FechaEnvio)
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
                        },
                        Usuario = new
                        {
                            p.Usuario.Id,
                            p.Usuario.Nombre,
                            p.Usuario.Apellido,
                            p.Usuario.AvatarUrl
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener propuestas: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener las propuestas.");
            }


        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Propuesta>> GetPropuestaById(int id)
        {
            try
            {
                var propuesta = await _context.Propuestas
        .Include(p => p.Usuario)
        .Include(p => p.Vacante)
        .FirstOrDefaultAsync(p => p.Id == id);

                if (propuesta == null)
                    return NotFound("Propuesta no encontrada");

                return Ok(propuesta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener propuesta por ID: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener la propuesta.");
            }

        }


        [HttpPost]
        public async Task<ActionResult<Propuesta>> CreatePropuesta([FromBody] Propuesta propuesta)
        {
            try
            {
                if (propuesta == null)
                    return BadRequest("Propuesta no puede ser nula");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var vacante = await _context.Vacantes.FindAsync(propuesta.VacanteId);
                if (vacante == null)
                    return NotFound("Vacante no encontrada");

                if (vacante.FechaExpiracion < DateTime.Now)
                    return BadRequest("No se pueden enviar propuestas a vacantes expiradas");

                if (!vacante.IsAbierta)
                    return BadRequest("La vacante ya no está abierta para recibir propuestas");

                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                propuesta.UsuarioId = usuarioId;

                _context.Propuestas.Add(propuesta);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPropuestaById), new { id = propuesta.Id }, propuesta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear propuesta: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al crear la propuesta.");

            }

        }


        [HttpPut("{id}")]
        public async Task<ActionResult<Propuesta>> UpdatePropuesta(int id, [FromBody] Propuesta propuesta)
        {

            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (id != propuesta.Id)
                    return BadRequest("ID de la propuesta no coincide");


                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var p = await _context.Propuestas.FindAsync(id);
                if (p == null)
                    return NotFound("Propuesta no encontrada");

                if (p.UsuarioId != usuarioId)
                    return Forbid("No tienes permiso para modificar esta propuesta");

                p.Mensaje = propuesta.Mensaje;
                p.Monto = propuesta.Monto;
                p.IsAceptada = propuesta.IsAceptada;
                p.IsRechazada = propuesta.IsRechazada;

                _context.Propuestas.Update(p);
                await _context.SaveChangesAsync();

                return Ok(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar propuesta: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al actualizar la propuesta.");
            }

        }

        [HttpPatch("{id}/aceptar")]
        public async Task<IActionResult> AceptarPropuesta(int id)
        {
            try
            {
                var propuesta = await _context.Propuestas.FindAsync(id);
                if (propuesta == null)
                    return NotFound();

                propuesta.IsAceptada = true;
                propuesta.IsRechazada = false;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener Aceptar Propuesta  {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al Aceptar Propuesta.");

            }

        }

    }
}