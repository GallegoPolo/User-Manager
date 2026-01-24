using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<UserCredential> UserCredentials => Set<UserCredential>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("ApiKeys");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(a => a.Status)
                      .IsRequired()
                      .HasConversion<int>();

                entity.Property(a => a.ExpiresAt)
                      .IsRequired(false);

                entity.Property(a => a.CreatedAt)
                      .IsRequired();

                entity.OwnsOne(a => a.KeyHash, keyHash =>
                {
                    keyHash.Property(h => h.Value)
                           .HasColumnName("KeyHash")
                           .IsRequired()
                           .HasMaxLength(500);

                    keyHash.HasIndex(h => h.Value)
                           .HasDatabaseName("IX_ApiKeys_KeyHash");
                });

                entity.OwnsMany(a => a.Scopes, scope =>
                {
                    scope.Property(s => s.Value)
                        .HasColumnName("Value")
                        .IsRequired()
                        .HasMaxLength(100);

                    scope.ToTable("ApiKeyScopes");
                });

                entity.Ignore(a => a.Notifications);
            });

            modelBuilder.Entity<UserCredential>(entity =>
            {
                entity.ToTable("UserCredentials");
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(u => u.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_UserCredentials_Email");

                entity.Property(u => u.Role)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(u => u.IsActive)
                      .IsRequired();

                entity.Property(u => u.CreatedAt)
                      .IsRequired();

                entity.Property(u => u.LastLoginAt)
                      .IsRequired(false);

                entity.OwnsOne(u => u.PasswordHash, passwordHash =>
                {
                    passwordHash.Property(h => h.Value)
                               .HasColumnName("PasswordHash")
                               .IsRequired()
                               .HasMaxLength(500);
                });

                entity.Ignore(u => u.Notifications);
            });
        }
    }
}

