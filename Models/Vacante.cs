using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class Vacante
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; }

        [Required]
        [StringLength(1000)]
        public string Descripcion { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Monto { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime? FechaExpiracion { get; set; }


        /// Indica si la vacante sigue abierta para recibir propuestas.
        /// Se pone a false cuando se acepta una propuesta.
        public bool IsAbierta { get; set; } = true;


        // Relaciones
        /// Usuario que publica la vacante.

        [Required]
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }


        public ICollection<Propuesta>? Propuestas { get; set; } = new List<Propuesta>();

    }
}