using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;
using UserManager.Infrastructure.Outbox;

namespace UserManager.Infrastructure.Persistence
{
    public class UserManagerDbContext : DbContext
    {
        public UserManagerDbContext(DbContextOptions<UserManagerDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(User.MAX_NAME_LENGTH);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(User.MAX_EMAIL_LENGTH);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Ignore(u => u.DomainEvents);
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.EventType).IsRequired().HasMaxLength(100);
                entity.Property(m => m.AggregateId).IsRequired().HasMaxLength(100);
                entity.Property(m => m.AggregateType).IsRequired().HasMaxLength(100);
                entity.Property(m => m.PayloadJson).IsRequired();
                entity.HasIndex(m => m.ProcessedAt); // Filtrar mais a query para processar
            });
        }

    }
}
