using MassTransit;
using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Infrastructure.Persistence
{
    public class UserManagerDbContext : DbContext
    {
        public UserManagerDbContext(DbContextOptions<UserManagerDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Contact> Contacts => Set<Contact>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name).IsRequired().HasMaxLength(User.MAX_NAME_LENGTH);
                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(Email.MaxLength)
                    .HasConversion(email => email.Value, value => Email.Create(value));
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Ignore(u => u.DomainEvents);
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("Contacts");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(Contact.MaxNameLength);
                entity.Property(c => c.Email)
                    .HasMaxLength(Email.MaxLength)
                    .HasConversion(email => email!.Value, value => Email.Create(value));
                entity.Property(c => c.Phone)
                    .HasMaxLength(PhoneNumber.MaxLength)
                    .HasConversion(phone => phone!.Value, value => PhoneNumber.Create(value));
                entity.HasIndex(c => new { c.OwnerId, c.Email }).IsUnique();
                entity.HasOne<User>().WithMany().HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Cascade);
                entity.Ignore(c => c.DomainEvents);
            });

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }

    }
}
