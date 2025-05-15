namespace TPFinalAvgustin.Models
{
    public abstract class RepositorioBase
    {
        protected readonly IConfiguration configuration;
        public readonly string connectionString;

        public RepositorioBase(IConfiguration configuration)
        {
            this.configuration = configuration;
            connectionString = configuration["ConnectionStrings:DefaultConnection"];
        }

    }
}