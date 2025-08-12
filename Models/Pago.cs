using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class Pago
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ContratoId { get; set; }

        [Required]
        public int ContratanteId { get; set; }

        [Required]
        public int ContratadoId { get; set; }

        [Required]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } // retenido, liberado, cancelado

        public DateTime? FechaPago { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relaciones (opcional si usás EF Navigation)
        public Contrato? Contrato { get; set; }
        public Usuario? Contratante { get; set; }
        public Usuario? Contratado { get; set; }
    }
}