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
            [FromQuery] int? usuarioId = null,
            [FromQuery] string? search = null)

        {
            var ID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var id = ID != null ? int.Parse(ID) : 0;
            var query = _context.Vacantes.AsQueryable();


            if (!string.IsNullOrEmpty(search))
                query = query.Where(v => v.Titulo.Contains(search));

            if (usuarioId != null)
            {
                query = query.Where(v => v.UsuarioId == usuarioId);
            }
            else
            {
                query = query.Where(v => v.UsuarioId != id);
            }
            Console.WriteLine("ID DEL USUARIO ENVIADO EN PETICION: " + usuarioId);

            var total = await query.CountAsync();
            var items = await query
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


        [HttpGet("{id}")]  // 7. GET /api/vacantes/{id}

        public async Task<ActionResult<Vacante>> GetVacanteById(int id)
        {
            var v = await _context.Vacantes.FindAsync(id);  // 8. Busca por PK
            if (v == null)
                return NotFound();                           // 9. Devuelve 404 si no existe
            return Ok(v);                                    // 10. Devuelve 200 OK + JSON
        }



        [HttpPost]  // 1. Asocia este método al verbo POST y a la ruta base /api/vacantes
                    // 2. (Opcional) si el controller ya tiene [AllowAnonymous], no es estrictamente necesario aquí
        public async Task<ActionResult<Vacante>> CreateVacante(
            [FromBody] Vacante vacante)  // 3. Model binding: deserializa el JSON del body a este parámetro
        {
            // 4. Marca la entidad como "nueva" en el contexto EF Core
            _context.Vacantes.Add(vacante);

            // 5. Inserta realmente en la base de datos dentro de una transacción
            await _context.SaveChangesAsync();

            // 6. Devuelve 201 Created, con cabecera Location apuntando al GET individual
            return Ok(vacante);                   // cuerpo de la respuesta: el objeto creado
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVacante(int id, [FromBody] Vacante dto)
        {
            var v = await _context.Vacantes.FindAsync(id);
            if (v == null) return NotFound();
            v.Titulo = dto.Titulo;
            v.Descripcion = dto.Descripcion;
            v.Monto = dto.Monto;
            v.FechaExpiracion = dto.FechaExpiracion;
            v.IsAbierta = dto.IsAbierta;
            await _context.SaveChangesAsync();
            return Ok(v); // 204
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVacante(int id)
        {
            var v = await _context.Vacantes.FindAsync(id);
            if (v == null) return NotFound();
            _context.Vacantes.Remove(v);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("mis-vacantes-con-propuestas")]
        [AllowAnonymous]
        public async Task<ActionResult> GetMisVacantesConPropuestas([FromQuery] int usuarioId)
        {
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

            return Ok(lista);
        }



    }
}