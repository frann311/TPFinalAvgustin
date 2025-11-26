using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VacantesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public VacantesController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]

        public async Task<ActionResult> GetVacantes(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] bool? mine = false,
            [FromQuery] string? search = null)

        {
            try
            {
                var query = _context.Vacantes.AsQueryable();
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


                if (!string.IsNullOrEmpty(search))
                    query = query.Where(v => v.Titulo.Contains(search));
                if (mine == true)
                {
                    query = query.Where(v => v.UsuarioId == usuarioId);
                }
                else
                {
                    query = query.Where(v => v.UsuarioId != usuarioId);
                }
                Console.WriteLine("ID DEL USUARIO ENVIADO EN PETICION: " + usuarioId);
                if (mine != true)
                {
                    query = query.Where(v => v.FechaExpiracion >= DateTime.Today);
                }


                var total = await query.CountAsync();
                var items = await query
                    .Where(v => v.IsAbierta)  //  Vacantes abiertas
                    .OrderBy(v => v.Id)
                    .Skip((page - 1) * size)
                    .Take(size)
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
                Console.WriteLine($"Error al obtener vacantes: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener las vacantes.");
            }

        }


        [HttpGet("{id}")]

        public async Task<ActionResult<Vacante>> GetVacanteById(int id)
        {

            try
            {
                var v = await _context.Vacantes.FindAsync(id);
                if (v == null)
                    return NotFound();
                return Ok(v);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener vacante por ID: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener la vacante.");
            }
        }



        [HttpPost]
        public async Task<ActionResult<Vacante>> CreateVacante(
            [FromBody] Vacante vacante)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                if (vacante.FechaExpiracion < DateTime.Now)
                    return BadRequest("La fecha de expiración no puede ser en el pasado");

                _context.Vacantes.Add(vacante);
                await _context.SaveChangesAsync();
                return Ok(vacante);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error al crear vacante: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al crear la vacante.");
            }

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVacante(int id, [FromBody] Vacante dto)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var v = await _context.Vacantes.FindAsync(id);
                if (v == null) return NotFound();
                if (v.UsuarioId != usuarioId)
                    return Forbid("No tienes permiso para modificar esta vacante");
                if (dto.FechaExpiracion < DateTime.Now)
                    return BadRequest("La fecha de expiración no puede ser en el pasado");
                if (!dto.IsAbierta)
                    return BadRequest("La vacante debe estar abierta para ser modificada");

                v.Titulo = dto.Titulo;
                v.Descripcion = dto.Descripcion;
                v.Monto = dto.Monto;
                v.FechaExpiracion = dto.FechaExpiracion;
                v.IsAbierta = dto.IsAbierta;
                await _context.SaveChangesAsync();
                return Ok(v); // 204
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar vacante: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al actualizar la vacante.");
            }

        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVacante(int id)
        {

            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var v = await _context.Vacantes.FindAsync(id);
                if (v == null)
                    return NotFound();

                if (v.UsuarioId != usuarioId)
                    return Forbid("No tienes permiso para eliminar esta vacante");

                if (!v.IsAbierta)
                    return BadRequest("La vacante debe estar abierta para ser eliminada");

                _context.Vacantes.Remove(v);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar vacante: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al eliminar la vacante.");
            }
        }


        [HttpGet("mis-vacantes-con-propuestas")]
        public async Task<ActionResult> GetMisVacantesConPropuestas()
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                Console.WriteLine("ID DEL USUARIO AUTENTICADO: " + usuarioId);

                // 2️⃣ Consultar las vacantes de ese usuario
                var lista = await _context.Vacantes
                    .AsNoTracking()
                    .Where(v => v.UsuarioId == usuarioId)
                    .Select(v => new
                    {
                        v.Id,
                        v.Titulo,
                        v.Descripcion,
                        PropuestasCount = v.Propuestas.Count()
                    })
                    .ToListAsync();

                // 3️⃣ Devolver resultado
                return Ok(lista);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error al obtener vacantes con propuestas: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener las vacantes con propuestas.");
            }

        }



    }
}