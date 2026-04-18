using Microsoft.EntityFrameworkCore;
using PinAppdePromo.Models;
namespace PinAppdePromo.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Negocio> Negocios { get; set; }
    }
}