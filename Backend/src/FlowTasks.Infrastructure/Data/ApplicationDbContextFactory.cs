using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlowTasks.Infrastructure.Data
{
    // L'outil EF Core cherchera cette implémentation
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var projectPath = Path.Combine(basePath, "..", "FlowTasks.Api");

            // 1. Configuration pour lire appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(projectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // 2. Récupère la chaîne de connexion
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 3. Configure les options pour PostgreSQL
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // ⭐ Changement important pour Npgsql/PostgreSQL ⭐
            optionsBuilder.UseNpgsql(connectionString);

            // 4. Retourne l'instance du DbContext
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}