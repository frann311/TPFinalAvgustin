using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TPFinalAvgustin.Models
{
    public class Archivo
    {
        public int Id { get; set; }

        // Relación con Contrato
        public int ContratoId { get; set; }
        public Contrato? Contrato { get; set; }

        // Información del archivo
        public string nombre_original { get; set; }
        public string nombre_sistema { get; set; }
        public string Url { get; set; }
        public string tipo_mime { get; set; }
        public long? tamanio { get; set; }

        // Opcional: quién subió el archivo

        // public Usuario Usuario { get; set; }
    }
}