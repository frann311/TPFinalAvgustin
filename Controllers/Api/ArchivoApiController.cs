using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Controllers.Api
{
    [ApiController]
    [Route("api/archivos")]
    [Authorize]
    public class ArchivoApiController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        public ArchivoApiController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> SubirArchivos(
            [FromForm] int contratoId,
            [FromForm] List<IFormFile> archivos
        )
        {
            try
            {
                if (archivos == null || archivos.Count == 0)
                {
                    return BadRequest(new { message = "No se seleccionaron archivos." });
                }
                var contrato = await _context.Contratos.FindAsync(contratoId);
                if (contrato == null)
                {
                    return NotFound(new { message = "Contrato no encontrado." });
                }
                if (!contrato.isActive || contrato.isCancelled)
                {
                    return BadRequest(new { message = "El contrato no está activo." });
                }
                string wwwRoot = _env.WebRootPath;
                string carpeta = Path.Combine(wwwRoot, "Uploads", "Contratos", contratoId.ToString());
                Directory.CreateDirectory(carpeta);

                var archivosGuardados = new List<Archivo>();

                foreach (var file in archivos)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName);
                        var nombreSistema = $"{Guid.NewGuid()}{ext}";
                        var rutaCompleta = Path.Combine(carpeta, nombreSistema);

                        // Guardar en disco
                        using var stream = new FileStream(rutaCompleta, FileMode.Create);
                        await file.CopyToAsync(stream);

                        // Guardar en BD
                        var archivo = new Archivo
                        {
                            ContratoId = contratoId,
                            nombre_original = file.FileName,
                            nombre_sistema = nombreSistema,
                            Url = $"/Uploads/Contratos/{contratoId}/{nombreSistema}",
                            tipo_mime = file.ContentType,
                            tamanio = file.Length// puedes completarlo si tienes usuario logueado
                        };

                        _context.Archivos.Add(archivo);
                        archivosGuardados.Add(archivo);
                    }

                }
                await _context.SaveChangesAsync();

                return Ok(archivosGuardados);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al subir archivos: " + ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }


        [HttpGet("{contratoId}")]
        public async Task<IActionResult> ObtenerArchivosPorContrato(int contratoId)
        {
            try
            {
                var archivos = await _context.Archivos
                .Where(a => a.ContratoId == contratoId)
                .Select(a => new
                {
                    a.Id,
                    a.nombre_original,
                    a.Url,
                    a.tipo_mime,
                    a.tamanio
                })
                .ToListAsync();

                return Ok(archivos);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener archivos: " + ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarArchivo(int id)
        {
            try
            {
                var archivo = await _context.Archivos.FindAsync(id);
                if (archivo == null)
                {
                    return NotFound(new { message = "Archivo no encontrado." });
                }
                var contrato = await _context.Contratos.FindAsync(archivo.ContratoId);
                if (contrato == null || !contrato.isActive || contrato.isCancelled)
                {
                    return BadRequest(new { message = "El contrato asociado no está activo." });
                }

                var rutaArchivo = Path.Combine(_env.WebRootPath, archivo.Url.TrimStart('/'));
                if (System.IO.File.Exists(rutaArchivo))
                {
                    System.IO.File.Delete(rutaArchivo);
                }

                _context.Archivos.Remove(archivo);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Archivo eliminado correctamente." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al eliminar el archivo: " + ex.Message);
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