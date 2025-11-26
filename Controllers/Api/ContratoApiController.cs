using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
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
    public class ContratoApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ContratoApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("en-curso")]
        public async Task<IActionResult> getContratos(
                    [FromQuery] int page = 1,
                    [FromQuery] int size = 10,

                    [FromQuery] string? search = null
                )


        {
            try
            {
                var ContratanteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
            catch (Exception ex)
            {

                Console.WriteLine($"Error al obtener los contratos en curso: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener los contratos en curso.");
            }


        }

        [HttpGet("{id}")]
        public async Task<IActionResult> getContratoById(int id)
        {
            try
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
        Monto = c.Propuesta.Monto,
        estado_disputa = c.estado_disputa
    })
    .FirstOrDefaultAsync();

                if (contrato == null)
                {
                    return NotFound("Contrato no encontrado");
                }

                return Ok(contrato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el contrato por ID: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener el contrato.");
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateContrato([FromBody] ContratoConMontoDTO dto)
        {

            Console.WriteLine("RAW BODY:");
            using (var reader = new StreamReader(Request.Body))
            {
                var body = await reader.ReadToEndAsync();
                Console.WriteLine(body);
            }
            try
            {
                if (dto?.Contrato == null)
                {
                    return BadRequest("Contrato no debe ser nulo");
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
                if (vacante == null)
                {
                    return NotFound("Vacante no encontrada");
                }



                var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                dto.Contrato.ContratanteId = usuarioId;
                vacante.IsAbierta = false;
                _context.Vacantes.Update(vacante);
                await _context.SaveChangesAsync();

                _context.Contratos.Add(dto.Contrato);
                await _context.SaveChangesAsync();


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


                return CreatedAtAction(nameof(getContratos), new { id = dto.Contrato.Id }, dto.Contrato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear el contrato: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al crear el contrato.");
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContrato(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var contrato = await _context.Contratos.FindAsync(id);
                if (contrato == null)
                {
                    return NotFound("Contrato no encontrado");
                }
                if (contrato.ContratanteId != userId)
                {
                    return Forbid("No tienes permiso para eliminar este contrato");
                }

                _context.Contratos.Remove(contrato);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar el contrato: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al eliminar el contrato.");
            }
        }



        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorialContratos
        (
                [FromQuery] int page = 1,

                [FromQuery] int size = 10,


                [FromQuery] string? search = null
                )
        {
            try
            {
                var ContratanteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el historial de contratos: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener el historial de contratos.");
            }


        }


        [HttpPost("cancelar/{id}")]
        public async Task<IActionResult> CancelarContrato(int id)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cancelar el contrato: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al cancelar el contrato.");
            }

        }




        [HttpPatch("finalizar/{id}")]
        public async Task<IActionResult> FinalizarContrato(int id)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al finalizar el contrato: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al finalizar el contrato.");
            }

        }


        [HttpGet("tasks")]
        public async Task<IActionResult> GetContratosByContratadoId(

            int page = 1,
            int size = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
                        ContratanteAvatar = c.Contratante.AvatarUrl,
                        isActive = c.isActive,
                        isCancelled = c.isCancelled
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
                Console.WriteLine($"Error al obtener los contratos: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener los contratos.");
            }

        }

        [HttpGet("taskById/{contratoId}")]
        public async Task<IActionResult> GetContratoByIdForTask(int contratoId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el contrato por ID para la tarea: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener el contrato.");
            }

        }


        [HttpGet("tasks/historial")]
        public async Task<IActionResult> GetHistorialContratosByContratadoId(

            int page = 1,
            int size = 10)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var baseQuery = _context.Contratos
                    .AsNoTracking()
                    .Include(c => c.Propuesta)
                        .ThenInclude(p => p.Vacante)
                    .Include(c => c.Contratante)
                    .Where(c => c.ContratadoId == userId && (c.isCancelled));

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
                        ContratanteAvatar = c.Contratante.AvatarUrl,
                        estado_disputa = c.estado_disputa,
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
                Console.WriteLine($"Error al obtener el historial de contratos: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener el historial de contratos.");
            }

        }

        [HttpPut("solicitar-revision/{taskId}")]
        public async Task<IActionResult> SolicitarRevision(int taskId)
        {

            try
            {
                var contrato = await _context.Contratos.FindAsync(taskId);
                if (contrato == null)
                {
                    return NotFound("Contrato no encontrado");
                }
                if (!contrato.isCancelled)
                {
                    return BadRequest("solo se puede solicitar revisión a contratos cancelados");
                }
                if (contrato.estado_disputa != "Ninguna")
                {
                    return BadRequest("Ya se ha solicitado una revisión para este contrato");
                }
                contrato.estado_disputa = "EnRevision";
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al solicitar revisión: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al solicitar la revisión.");
            }

        }



        [HttpGet("disputes")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetDisputedContracts(int page = 1, int size = 10)
        {
            try
            {
                var baseQuery = _context.Contratos
    .AsNoTracking()
    .Include(c => c.Propuesta)
        .ThenInclude(p => p.Vacante)
    .Include(c => c.Contratante)
    .Where(c => c.estado_disputa == "EnRevision");

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener contratos en disputa: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener los contratos en disputa.");
            }

        }

        [HttpGet("disputesById/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetDisputedContractById(int id)
        {
            try
            {
                var contrato = await _context.Contratos
    .AsNoTracking()
    .Include(c => c.Propuesta)
        .ThenInclude(p => p.Vacante)
    .Include(c => c.Contratante)
    .Include(c => c.Contratado)
    .Where(c => c.Id == id && c.estado_disputa == "EnRevision")
    .Select(c => new
    {
        c.Id,
        FechaCreacion = c.FechaCreacion,
        FechaFinalizacion = c.FechaFinalizacion,
        FechaLimite = c.FechaLimite,
        VacanteTitulo = c.Propuesta.Vacante.Titulo,
        VacanteDescripcion = c.Propuesta.Vacante.Descripcion,
        ContratanteNombre = c.Contratante.Nombre,
        ContratanteApellido = c.Contratante.Apellido,

        ContratadoNombre = c.Contratado.Nombre,
        ContratadoApellido = c.Contratado.Apellido,



    })
    .FirstOrDefaultAsync();

                if (contrato == null)
                {
                    return NotFound("Contrato en disputa no encontrado");
                }

                return Ok(contrato);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el contrato en disputa: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al obtener el contrato en disputa.");
            }

        }




        [HttpPatch("descartar/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DescartarRevision(int id)
        {
            try
            {
                var contrato = await _context.Contratos.FindAsync(id);
                if (contrato == null) return NotFound();

                contrato.estado_disputa = "Rechazada";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Revisión descartada correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descartar la revisión {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al descartar la revisión.");

            }

        }


        [HttpPatch("resolver/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ResolverContrato(int id, [FromBody] ResolverContratoDto dto)
        {

            try
            {
                var contrato = await _context.Contratos.FindAsync(id);
                if (contrato == null)
                    return NotFound("Contrato no encontrado");

                if (contrato.estado_disputa != "EnRevision")
                    return BadRequest("El contrato no está en estado de revisión");

                if (dto.Porcentaje < 0 || dto.Porcentaje > 100)
                    return BadRequest("El porcentaje debe estar entre 0 y 100");

                var pago = await _context.Pagos
                    .Where(p => p.ContratoId == contrato.Id && p.Estado == "cancelado")
                    .FirstOrDefaultAsync();

                if (pago == null)
                    return BadRequest("No se puede resolver un contrato sin pago asociado");

                if (pago.Estado != "cancelado")
                    return BadRequest("El pago asociado al contrato no está en estado cancelado");

                pago.Estado = "liberado";
                pago.Monto = (pago.Monto * dto.Porcentaje) / 100;

                contrato.estado_disputa = "Aprobada";
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al resolver contrato: {ex.Message}");
                return StatusCode(500, "Ocurrió un error inesperado al resolver el contrato.");
            }

        }



    }




}