

namespace Habitera.Models
{
    public class VerificationOptions
    {
        public int EmailCodeExpiryMinutes { get; set; }
        public int MaxAttempts { get; set; }
        public int ResendCooldownSeconds { get; set; } = 60;
        public int PasswordResetCodeExpiryMinutes { get; set; } = 15;
    }

    public class ElasticsearchSettings
    {
        public string Uri { get; set; } = "http://localhost:9200";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ApiKey { get; set; }
        public bool EnableDebugMode { get; set; }
    }

}