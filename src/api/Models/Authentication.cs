using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Identity;
using Habitera.DTOs;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


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

        public bool ProfileCompleted { get; set; } = false;
        public DateTime? ProfileCompletedAt { get; set; }

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

    public class UserPreferences
    {
        public PriceRange AveragePriceRange { get; set; } = new();
        public int PreferredBedroomCount { get; set; }
        public int PreferredBathroomCount { get; set; }
        public List<PropertyType> PreferredPropertyTypes { get; set; } = new();
        public List<string> PreferredCities { get; set; } = new();
        public List<string> PreferredStates { get; set; } = new();
        public List<string> PreferredAmenities { get; set; } = new();
        public decimal? PreferredSquareFeet { get; set; }
        public decimal? MaxDistanceFromWork { get; set; } // in km
        public GeoLocation? WorkLocation { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PriceRange
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }

        public bool IsInRange(decimal price)
        {
            return price >= Min && price <= Max;
        }

        public decimal Average => (Min + Max) / 2;

        public decimal Range => Max - Min;
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

    public class UserInteraction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PropertyId { get; set; }
        public InteractionType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DurationSeconds { get; set; } // How long they viewed
        public string? DeviceType { get; set; }
        public string? Source { get; set; } // search, recommendation, etc.
    }

    public enum InteractionType
    {
        View = 0,
        Favorite = 1,
        Unfavorite = 2,
        Share = 3,
        ContactAgent = 4,
        BookViewing = 5,
        SavedSearch = 6,
        Click = 7
    }

    public class RecommendationScore
    {
        public Guid PropertyId { get; set; }
        public PropertyDTO Property { get; set; } = new();
        public double Score { get; set; }
        public Dictionary<string, double> FactorScores { get; set; } = new();
        public string Reason { get; set; } = string.Empty;

    }

    public class SearchPattern
    {
        public Guid UserId { get; set; }
        public int TotalSearches { get; set; }
        public List<string> FrequentKeywords { get; set; } = new();
        public PriceRange CommonPriceRange { get; set; } = new();
        public List<string> PreferredLocations { get; set; } = new();
        public DateTime FirstSearchDate { get; set; }
        public DateTime LastSearchDate { get; set; }
    }

    public class PricePoint
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string? ChangeReason { get; set; } // e.g., "Price reduced", "Market adjustment"
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

