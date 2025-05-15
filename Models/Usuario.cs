using System.ComponentModel.DataAnnotations;

namespace TPFinalAvgustin.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } // Usado solo para el formulario (no se guarda)

        public string PasswordHash { get; set; } // Esto sí se guarda

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string Apellido { get; set; }

        public string Rol { get; set; } = "Common"; // Rol por defecto

        public string? AvatarUrl { get; set; }

        public string? Perfil { get; set; }

        public DateTime Creado_En { get; set; }

    }
}
