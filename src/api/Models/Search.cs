using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using Elastic.Clients.Elasticsearch;
using NetTopologySuite;

namespace Habitera.Models
{
    public class PropertyDocument
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string PropertyType { get; set; } = null!;
        public string ListingType { get; set; } = null!;

        public string Street { get; set; } = null!;
        public string City { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public string FullAddress { get; set; } = null!;

        public GeoLocation Location { get; set; } = null!;

        public int Bedrooms { get; set; } = 0;
        public decimal Bathrooms { get; set; } = 0;
        public decimal SquareFeet { get; set; } = 0;
        public decimal LotSize { get; set; } = 0;
        public int? YearBuilt { get; set; } 

        public decimal Price { get; set; }
        public string Currency { get; set; } = "NGN";

        public string Status { get; set; } = null!;
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }

        public int ViewCount { get; set; }
        public int FavoriteCount { get; set; }

        public List<PropertyImageDocument> Images { get; set; } = new();
        public string? PrimaryImageUrl { get; set; }

        public List<string> AmenityTags { get; set; } = new();
        public Dictionary<string, object> AmenitiesData { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        public double PricePerSquareFoot { get; set; }
        public int DaysOnMarket { get; set; }
    }

    public class GeoCoordinates
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class PropertyImageDocument
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string? ThumbnailUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class PropertySearchRequest
    {
        public string? Query { get; set; }

        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }

        public GeoSearchFilter? GeoSearch { get; set; }

        public List<string>? PropertyTypes { get; set; }
        public List<string>? ListingTypes { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinBedrooms { get; set; }
        public int? MaxBedrooms { get; set; }
        public decimal? MinBathrooms { get; set; }
        public decimal? MaxBathrooms { get; set; }
        public decimal? MinSquareFeet { get; set; }
        public decimal? MaxSquareFeet { get; set; }

        public List<string>? Amenities { get; set; }

        public List<string>? Statuses { get; set; }
        public bool? IsFeatured { get; set; }

        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class GeoSearchFilter
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }

        public GeoBoundingBox? BoundingBox { get; set; }
    }

    public class GeoBoundingBox
    {
        public double TopLeftLat { get; set; }
        public double TopLeftLon { get; set; }
        public double BottomRightLat { get; set; }
        public double BottomRightLon { get; set; }
    }

    public class PropertySearchResponse
    {
        public List<PropertyDocument> Properties { get; set; } = new();
        public long TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public SearchAggregations? Aggregations { get; set; }
    }

    public class SearchAggregations
    {
        public Dictionary<string, long>? PropertyTypes { get; set; }
        public Dictionary<string, long>? Cities { get; set; }
        public PriceRangeAggregation? PriceRange { get; set; }
    }

    public class PriceRangeAggregation
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }
    }
}