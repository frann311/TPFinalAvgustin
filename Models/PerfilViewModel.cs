
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TPFinalAvgustin.Models
{
    public class PerfilViewModel
    {
        public int Id { get; set; }

        [Required] public string Nombre { get; set; }
        [Required] public string Apellido { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

               public string? Perfil { get; set; }

        // URL actual del avatar (p.ej. "/Avatars/5/foo.jpg")
        public string? AvatarUrl { get; set; }

        // Para subir uno nuevo
        [Display(Name = "Cambiar foto")]
        public IFormFile? AvatarFile { get; set; }

        // Para indicar que quiere quitar el avatar
        public bool RemoveAvatar { get; set; }
        public string Rol { get; set; } = "Empleado"; // Rol por defecto

        // Campos para cambiar contraseña (opcionales)
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }
}
