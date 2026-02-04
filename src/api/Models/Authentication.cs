using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using NetTopologySuite;

namespace Habitera.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ApplicationUser : IdentityUser<Guid>
    {

        [Required]
        public UserType UserType { get; set; } = UserType.User;

        public UserStatus Status { get; set; } = UserStatus.New;

        [MaxLength(500)]
        public string? ProfilePhoto { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }
        public string? DeletionReason { get; set; }

        public required UserProfile Profile { get; set; }
        public AgentProfile? AgentProfile { get; set; }

    }

    public enum UserType
    {
        User = 0,
        Agent = 1,
        Admin = 2
    }

    public enum UserStatus
    {
        Deleted = 0,
        Inactive = 1,
        Locked = 2,
        Suspended = 3,
        Active = 4,
        Pending = 5,
        New = 6,
    }

    public class UserProfile
    {
        [Key]
        public Guid UserId { get; set; }
        public required ApplicationUser User { get; set; }

        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public required string City { get; set; }
        public required string State { get; set; }
        public required string Country { get; set; }

        public required Point Location { get; set; }

        public string PreferredLanguage { get; set; } = "en";
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
    }

    public class AgentProfile
    {
        [Key]
        public Guid UserId { get; set; }
        public required ApplicationUser User { get; set; }

        public string? LicenseNumber { get; set; }
        public string? AgencyName { get; set; }

        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }

    }

    public class PasswordResetToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
        public DateTime UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string? PlainCode { get; set; }
    }

    public class EmailVerificationToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required string Email { get; set; }
        public required string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public string? PlainCode { get; set; }
    }

    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }

        [Required, MaxLength(255)]
        public required string TokenHash { get; set; }

        [MaxLength(500)]
        public string? DeviceInfo { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime? RevokedAt { get; set; }

        [MaxLength(200)]
        public string? RevokedReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required Guid UserId { get; set; }
        public required string Action { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

