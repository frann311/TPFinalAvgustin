using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class Propuesta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Mensaje { get; set; }

        [Required]
        public decimal Monto { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        public bool IsAceptada { get; set; } = false;

        public bool IsRechazada { get; set; } = false;

        // Relaciones

        [Required]
        public int UsuarioId { get; set; } // Contratado (quien envía la propuesta)
        [JsonIgnore]
        public Usuario? Usuario { get; set; }

        [Required]
        public int VacanteId { get; set; }
        [JsonIgnore]
        public Vacante? Vacante { get; set; }
    }
}