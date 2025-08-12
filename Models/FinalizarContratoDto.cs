using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class FinalizarContratoDto
    {
        public bool IsActive { get; set; }
        public bool IsCanceled { get; set; }
        public DateTime FechaFinalizacion { get; set; }
        public bool ActualizarPago { get; set; }
    }
}