using Habitera.Models;
using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace Habitera.DTOs
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }

        public UserType UserType { get; set; }
        public UserStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string PreferredLanguage { get; set; } = "en";
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;

        public string? LicenseNumber { get; set; }
        public string? AgencyName { get; set; }
        public decimal? AverageRating { get; set; }
        public int? TotalReviews { get; set; }
    }

    public class AuthResponseDTO
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public UserDTO? User { get; set; }
    }

    public abstract class DeviceInfoRequestDTO
    {
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class RegisterRequestDTO : DeviceInfoRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserType UserType { get; set; }
    }

    public class LoginRequestDTO : DeviceInfoRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest : DeviceInfoRequestDTO
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }

    public class VerifyEmailRequestDTO : DeviceInfoRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }

    public class ResendCodeRequestDTO : DeviceInfoRequestDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ForgotPasswordDTO : DeviceInfoRequestDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDTO : DeviceInfoRequestDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequestDTO : DeviceInfoRequestDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class DeleteAccountRequestDTO : DeviceInfoRequestDTO
    {
        public string Password { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}


