using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/contratos")]
    [Authorize]
    public class ContratoApiController : Controller
    {
        private readonly AppDbContext _context;
        public ContratoApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("en-curso/{ContratanteId}")]
        public async Task<IActionResult> getContratos(
                    [FromQuery] int page = 1,
                    [FromQuery] int size = 10,
                    [FromRoute] int ContratanteId = 0,
                    [FromQuery] string? search = null
                )

        {
            Console.WriteLine("ID DEL CONTRATANTE ENVIADO EN PETICION: ", ContratanteId);
            var baseQuery = _context.Contratos
                .AsNoTracking()
                .Where(c => c.ContratanteId == ContratanteId)
                .Where(c => c.isActive && !c.isCancelled); // Filtrar por contratos activos y no cancelados
            if (search != null)
                baseQuery = baseQuery.Where(c => c.Propuesta.Mensaje.Contains(search));

            var total = await baseQuery.CountAsync();
            var items = await baseQuery
                .OrderBy(c => c.Id)
                .Skip((page - 1) * size)
                .Take(size)

                .Select(c => new
                {
                    c.Id,
                    PropuestaId = c.Propuesta.Id,
                    PropuestaMensaje = c.Propuesta.Mensaje,
                    isActive = c.isActive,
                    isCancelled = c.isCancelled,
                    FechaCreacion = c.FechaCreacion,
                    FechaFinalizacion = c.FechaFinalizacion,
                    FechaLimite = c.FechaLimite,
                    VacanteTitulo = c.Propuesta.Vacante.Titulo,
                    ContratanteNombre = c.Contratante.Nombre,
                    ContratadoNombre = c.Contratado.Nombre,
                    contratadoApellido = c.Contratado.Apellido,
                    contratadoAvatar = c.Contratado.AvatarUrl,
                    contratadoId = c.ContratadoId
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

        [HttpGet("{id}")]
        public async Task<IActionResult> getContratoById(int id)
        {
            var contrato = await _context.Contratos
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    PropuestaId = c.Propuesta.Id,
                    PropuestaMensaje = c.Propuesta.Mensaje,
                    isActive = c.isActive,
                    isCancelled = c.isCancelled,
                    FechaCreacion = c.FechaCreacion,
                    FechaFinalizacion = c.FechaFinalizacion,
                    FechaLimite = c.FechaLimite,
                    VacanteTitulo = c.Propuesta.Vacante.Titulo,
                    VacanteDescripcion = c.Propuesta.Vacante.Descripcion,
                    ContratanteNombre = c.Contratante.Nombre,
                    contratanteApellido = c.Contratante.Apellido,
                    ContratadoNombre = c.Contratado.Nombre,
                    contratadoApellido = c.Contratado.Apellido,
                    contratadoAvatar = c.Contratado.AvatarUrl,
                    contratadoId = c.ContratadoId,
                    Monto = c.Propuesta.Monto
                })
                .FirstOrDefaultAsync();

            if (contrato == null)
            {
                return NotFound("Contrato no encontrado");
            }

            return Ok(contrato);
        }

        [HttpPost]
        public async Task<IActionResult> CreateContrato([FromBody] ContratoConMontoDTO dto)
        {
            if (dto?.Contrato == null)
            {
                return BadRequest("Contrato cannot be null");
            }
            var propuesta = await _context.Propuestas
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.Contrato.PropuestaId);
            if (propuesta == null)
            {
                return NotFound("Propuesta no encontrada");
            }

            var vacante = await _context.Vacantes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == propuesta.VacanteId);
            // actualizar el estado de la vacante a "ocupada"
            if (vacante == null)
            {
                return NotFound("Vacante no encontrada");
            }
            vacante.IsAbierta = false;
            _context.Vacantes.Update(vacante);
            await _context.SaveChangesAsync();
            // 1. Guardar el contrato
            _context.Contratos.Add(dto.Contrato);
            await _context.SaveChangesAsync();

            // 2. Crear el pago relacionado
            var pago = new Pago
            {
                ContratoId = dto.Contrato.Id,
                ContratanteId = dto.Contrato.ContratanteId,
                ContratadoId = dto.Contrato.ContratadoId,
                Monto = dto.Monto,
                Estado = "retenido",
                FechaPago = null,
                FechaCreacion = DateTime.Now
            };

            _context.Pagos.Add(pago);
            await _context.SaveChangesAsync();

            // 3. Retornar el contrato creado
            return CreatedAtAction(nameof(getContratos), new { id = dto.Contrato.Id }, dto.Contrato);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContrato(int id)
        {
            var contrato = await _context.Contratos.FindAsync(id);
            if (contrato == null)
            {
                return NotFound("Contrato no encontrado");
            }

            _context.Contratos.Remove(contrato);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        [HttpGet("historial/{ContratanteId}")]
        public async Task<IActionResult> GetHistorialContratos
        (
                [FromQuery] int page = 1,

                [FromQuery] int size = 10,

                [FromRoute] int ContratanteId = 0,

                [FromQuery] string? search = null
                )
        {
            Console.WriteLine("ID DEL CONTRATANTE ENVIADO EN PETICION PARA EL: ", ContratanteId);
            var baseQuery = _context.Contratos
                .AsNoTracking()
                .Where(c => c.ContratanteId == ContratanteId)
                .Where(c => c.isCancelled || !c.isActive); // Filtrar por contratos cancelados o inactivos
            if (search != null)
                baseQuery = baseQuery.Where(c => c.Propuesta.Mensaje.Contains(search));

            var total = await baseQuery.CountAsync();
            var items = await baseQuery
                .OrderBy(c => c.Id)
                .Skip((page - 1) * size)
                .Take(size)

                .Select(c => new
                {
                    c.Id,
                    PropuestaId = c.Propuesta.Id,
                    PropuestaMensaje = c.Propuesta.Mensaje,
                    isActive = c.isActive,
                    isCancelled = c.isCancelled,
                    FechaCreacion = c.FechaCreacion,
                    FechaFinalizacion = c.FechaFinalizacion,
                    FechaLimite = c.FechaLimite,
                    VacanteTitulo = c.Propuesta.Vacante.Titulo,
                    ContratanteNombre = c.Contratante.Nombre,
                    ContratadoNombre = c.Contratado.Nombre,
                    contratadoApellido = c.Contratado.Apellido,
                    contratadoAvatar = c.Contratado.AvatarUrl,
                    contratadoId = c.ContratadoId
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


        [HttpPost("cancelar/{id}")]
        public async Task<IActionResult> CancelarContrato(int id)
        {
            var contrato = await _context.Contratos.FindAsync(id);
            if (contrato == null)
            {
                return NotFound("Contrato no encontrado");
            }
            if (contrato.isCancelled)
            {
                return BadRequest("El contrato ya está cancelado");
            }
            if (!contrato.isActive)
            {
                return BadRequest("El contrato ya está inactivo");
            }
            if (contrato.FechaLimite < DateTime.Now)
            {
                return BadRequest("No se puede cancelar un contrato que ya ha expirado");
            }
            var pago = await _context.Pagos
                .Where(p => p.ContratoId == contrato.Id && p.Estado == "retenido")
                .FirstOrDefaultAsync();
            if (pago == null)
            {
                return BadRequest("No se puede cancelar un contrato sin pago asociado");
            }
            if (pago.Estado != "retenido")
            {
                return BadRequest("El pago asociado al contrato no está en estado retenido");
            }
            pago.Estado = "cancelado";
            contrato.FechaFinalizacion = DateTime.Now;
            contrato.isCancelled = true;
            contrato.isActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }




        [HttpPatch("finalizar/{id}")]
        public async Task<IActionResult> FinalizarContrato(int id)
        {
            var contrato = await _context.Contratos.FindAsync(id);
            if (contrato == null)
            {
                return NotFound("Contrato no encontrado");
            }
            if (contrato.isCancelled)
            {
                return BadRequest("El contrato ya está cancelado");
            }
            if (!contrato.isActive)
            {
                return BadRequest("El contrato ya está inactivo");
            }

            var pago = await _context.Pagos
    .Where(p => p.ContratoId == contrato.Id && p.Estado == "retenido")
    .FirstOrDefaultAsync();
            if (pago == null)
            {
                return BadRequest("No se puede cancelar un contrato sin pago asociado");
            }
            if (pago.Estado != "retenido")
            {
                return BadRequest("El pago asociado al contrato no está en estado retenido");
            }
            pago.Estado = "liberado";
            contrato.FechaFinalizacion = DateTime.Now;
            contrato.isCancelled = false;
            contrato.isActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }


        [HttpGet("tasks/{userId}")]
        public async Task<IActionResult> GetContratosByContratadoId(
            int userId,
            int page = 1,
            int size = 10)
        {
            var baseQuery = _context.Contratos
                .AsNoTracking()
                .Include(c => c.Propuesta)
                    .ThenInclude(p => p.Vacante)
                .Include(c => c.Contratante)
                .Where(c => c.ContratadoId == userId && c.isActive && !c.isCancelled);

            var total = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderBy(c => c.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(c => new
                {
                    c.Id,
                    FechaCreacion = c.FechaCreacion,
                    FechaFinalizacion = c.FechaFinalizacion,
                    FechaLimite = c.FechaLimite,
                    VacanteTitulo = c.Propuesta.Vacante.Titulo,
                    ContratanteNombre = c.Contratante.Nombre,
                    ContratanteApellido = c.Contratante.Apellido,
                    ContratanteAvatar = c.Contratante.AvatarUrl
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

        [HttpGet("taskById/{contratoId}")]
        public async Task<IActionResult> GetContratoByIdForTask(int contratoId)
        {
            var contrato = await _context.Contratos
                .AsNoTracking()
                .Include(c => c.Propuesta)
                    .ThenInclude(p => p.Vacante)
                .Include(c => c.Contratante)
                .Include(c => c.Contratado)
                .Where(c => c.Id == contratoId && c.isActive && !c.isCancelled)
                .Select(c => new
                {
                    c.Id,
                    FechaCreacion = c.FechaCreacion,
                    FechaFinalizacion = c.FechaLimite,
                    FechaLimite = c.FechaLimite,
                    VacanteTitulo = c.Propuesta.Vacante.Titulo,
                    VacanteDescripcion = c.Propuesta.Vacante.Descripcion,
                    ContratanteNombre = c.Contratante.Nombre,
                    ContratanteApellido = c.Contratante.Apellido,
                    ContratanteAvatar = c.Contratante.AvatarUrl,
                    ContratadoNombre = c.Contratado.Nombre,
                    ContratadoApellido = c.Contratado.Apellido,
                    ContratadoAvatar = c.Contratado.AvatarUrl
                })
                .FirstOrDefaultAsync();

            if (contrato == null)
            {
                return NotFound("Contrato no encontrado");
            }

            return Ok(contrato);
        }



    }




}