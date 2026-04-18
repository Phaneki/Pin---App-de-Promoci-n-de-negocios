using Microsoft.EntityFrameworkCore;

namespace Pin__App_de_Promoci_n_de_negocios.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Negocio> Negocios { get; set; }
    }
}