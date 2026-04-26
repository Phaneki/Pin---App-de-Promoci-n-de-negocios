using Microsoft.EntityFrameworkCore;

namespace PinAppdePromo.Models
{
    public class PinDbContext : DbContext
    {
        public PinDbContext(DbContextOptions<PinDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessImage> BusinessImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<StaffLog> StaffLogs { get; set; }
        public DbSet<BusinessReport> BusinessReports { get; set; }
        public DbSet<BusinessMetric> BusinessMetrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de llaves compuestas
            modelBuilder.Entity<UserLogin>()
                .HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });

            modelBuilder.Entity<Favorite>()
                .HasKey(f => new { f.UserId, f.BusinessId });

            // Relación Business -> Category
            modelBuilder.Entity<Business>()
                .HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId);

            // Relación Business -> Owner (User)
            modelBuilder.Entity<Business>()
                .HasOne(b => b.Owner)
                .WithMany(u => u.OwnedBusinesses)
                .HasForeignKey(b => b.OwnerId);

            // Relación Review -> Business
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Business)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BusinessId);

            // Relación Review -> User
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);
                
            // Evitar problemas de borrado en cascada múltiple
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}