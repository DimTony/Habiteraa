using Habitera.Models;
using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace Habitera.DTOs
{
    public class CompleteRegularUserProfileDTO : DeviceInfoRequestDTO
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? PhoneNumber { get; set; }
        public string PreferredLanguage { get; set; } = "en";
    }

    public class CompleteAgentProfileDTO : DeviceInfoRequestDTO
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string? LicenseNumber { get; set; }
        public string? AgencyName { get; set; }
        public string? PhoneNumber { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Country { get; set; } = string.Empty;
    }

    public class ProfileCompletionStatusDTO
    {
        public bool IsProfileCompleted { get; set; }
        public UserType UserType { get; set; }
        public List<string> MissingFields { get; set; } = new();
        public int CompletionPercentage { get; set; }
    }
}