using Elastic.Clients.Elasticsearch;
using Habitera.DTOs;
using Microsoft.AspNetCore.Identity;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Habitera.Models
{

    public class GeoCoordinates
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
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
        public List<PropertyDTO> Properties { get; set; } = new();
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