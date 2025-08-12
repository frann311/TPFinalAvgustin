using Microsoft.EntityFrameworkCore;
using TPFinalAvgustin.Models;

namespace TPFinalAvgustin.Models
{
    // Contexto EF Core: representa la base de datos
    public class AppDbContext : DbContext
    {
        // Constructor con opciones (cadena de conexión, provider…)
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // DbSet = colección de Vacantes en la BD

        public DbSet<Archivo> Archivos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Contrato> Contratos { get; set; }
        public DbSet<Vacante> Vacantes { get; set; }
        public DbSet<Propuesta> Propuestas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

    }
}
