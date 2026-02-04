using Habitera.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection.Emit;

namespace Habitera.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<AgentProfile> AgentProfiles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.UserType)
                      .HasConversion<int>()
                      .IsRequired();

                entity.Property(u => u.Status)
                      .HasConversion<int>()
                      .IsRequired();

                entity.HasIndex(u => u.UserType);
                entity.HasIndex(u => u.Status);
            });

            builder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(p => p.UserId);

                entity.HasOne(p => p.User)
                      .WithOne(u => u.Profile)
                      .HasForeignKey<UserProfile>(p => p.UserId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(p => p.FirstName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(p => p.LastName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(p => p.City)
                      .HasMaxLength(100);

                entity.Property(p => p.State)
                      .HasMaxLength(100);

                entity.Property(p => p.Country)
                      .HasMaxLength(100);


                entity.Property(p => p.Location);
            });

            builder.Entity<AgentProfile>(entity =>
            {
                entity.HasKey(a => a.UserId);

                entity.HasOne(a => a.User)
                      .WithOne(u => u.AgentProfile)
                      .HasForeignKey<AgentProfile>(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(a => a.AgencyName)
                      .HasMaxLength(200);

                entity.HasIndex(a => a.LicenseNumber)
                      .IsUnique()
                      .HasFilter("\"LicenseNumber\" IS NOT NULL");
            });


        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

}