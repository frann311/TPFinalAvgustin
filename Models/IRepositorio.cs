namespace TPFinalAvgustin.Models

{

    public interface IRepositorio<T>
    {
        IList<T> ObtenerTodos();
        T ObtenerPorId(int id);
        int Alta(T p);
        int Baja(int id);
        int Modificacion(T entidad);
    }

}