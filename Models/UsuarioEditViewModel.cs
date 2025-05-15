using System.ComponentModel.DataAnnotations;
namespace TPFinalAvgustin.Models
{
    public class UsuarioEditViewModel
    {
        public int Id { get; set; }

        [Required] public string Nombre { get; set; }
        [Required] public string Apellido { get; set; }
        [Required][EmailAddress] public string Email { get; set; }

        [Required] public string Rol { get; set; }

        // **No ponemos Required** ni Compare aquí:
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }
}
