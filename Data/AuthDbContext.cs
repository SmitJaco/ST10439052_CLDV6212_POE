using Microsoft.EntityFrameworkCore;
using ST10439052_CLDV_POE.Models;

namespace ST10439052_CLDV_POE.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Cart { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User table
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.Username).HasColumnName("Username").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Role).HasColumnName("Role").HasMaxLength(20).IsRequired();
            });

            // Configure Cart table
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Cart");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.CustomerUsername).HasColumnName("CustomerUsername").HasMaxLength(100);
                entity.Property(e => e.ProductId).HasColumnName("ProductId").HasMaxLength(100);
                entity.Property(e => e.Quantity).HasColumnName("Quantity");
            });
        }
    }
}

