using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

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
                });

                entity.OwnsMany(a => a.Scopes, scope =>
                {
                    scope.Property(s => s.Value)
                        .HasColumnName("Value")
                        .IsRequired()
                        .HasMaxLength(100);
                    scope.ToTable("ApiKeyScopes");
                });

                entity.HasIndex(a => a.KeyHash.Value)
                      .HasDatabaseName("IX_ApiKeys_KeyHash");

                entity.Ignore(a => a.Notifications);
            });
        }
    }
}
