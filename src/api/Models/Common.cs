

namespace Habitera.Models
{
    public class VerificationOptions
    {
        public int EmailCodeExpiryMinutes { get; set; }
        public int MaxAttempts { get; set; }
        public int ResendCooldownSeconds { get; set; } = 60;
        public int PasswordResetCodeExpiryMinutes { get; set; } = 15;
    }
}