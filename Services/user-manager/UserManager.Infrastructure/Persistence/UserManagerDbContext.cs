using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;

namespace UserManager.Infrastructure.Persistence
{
    public class UserManagerDbContext : DbContext
    {
        public UserManagerDbContext(DbContextOptions<UserManagerDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Ignore(u => u.Notifications);
            });
        }

    }
}
