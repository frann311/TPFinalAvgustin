using System.Collections.Generic;

namespace TPFinalAvgustin.Models
{
    public interface IRepositorioUsuario : IRepositorio<Usuario>
    {
        Usuario ObtenerPorEmail(string email);

    }
}
