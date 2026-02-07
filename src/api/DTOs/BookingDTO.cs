using Habitera.Models;

namespace Habitera.DTOs
{
    public class BookingDTO
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public Guid UserId { get; set; }
        public Guid AgentId { get; set; }

        // Property details (for user's booking list)
        public PropertySummaryDTO? Property { get; set; }

        // User details (for agent's booking list)
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhone { get; set; }

        public DateTime ScheduledDate { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Helper properties
        public bool CanCancel => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
        public bool CanConfirm => Status == BookingStatus.Pending;
        public bool IsPast => ScheduledDate < DateTime.UtcNow;
        public bool IsUpcoming => ScheduledDate >= DateTime.UtcNow &&
                                  (Status == BookingStatus.Confirmed || Status == BookingStatus.Pending);
    }

    public class PropertySummaryDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "NGN";
        public string? PrimaryImageUrl { get; set; }
        public PropertyType PropertyType { get; set; }
        public ListingType ListingType { get; set; }
        public int Bedrooms { get; set; }
        public decimal Bathrooms { get; set; }
    }
}
