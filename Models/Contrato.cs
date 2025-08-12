using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class Contrato
    {
        public int Id { get; set; }

        // Relación con la propuesta aceptada
        public int PropuestaId { get; set; }
        public Propuesta? Propuesta { get; set; }

        // Usuario que contrató (el que creó la vacante)

        public int ContratanteId { get; set; }
        public Usuario? Contratante { get; set; }

        public int ContratadoId { get; set; }
        public Usuario? Contratado { get; set; }

        // Estado del contrato (por ejemplo: Activo, Finalizado, Disputado, Cancelado)
        public bool isActive { get; set; }
        public bool isCancelled { get; set; }

        // Fechas importantes
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaLimite { get; set; }
        public DateTime? FechaFinalizacion { get; set; }


    }
}