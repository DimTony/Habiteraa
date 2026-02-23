using Habitera.DTOs;
using Habitera.Models;
using Habitera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;

namespace Habitera.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly IPropertyService _propertyService;
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyService propertyService,
            IRecommendationService recommendationService,
            ILogger<PropertyController> logger)
        {
            _propertyService = propertyService;
            _recommendationService = recommendationService;
            _logger = logger;
        }

        [HttpGet("Search/Active")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> SearchActiveProperties([FromQuery] PaginatedRequest request)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var statuses = new[] { PropertyStatus.Active, PropertyStatus.Sold };
            var result = await _propertyService.GetAgentPropertiesByStatusesAsync(agentId, request, statuses);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Search/UnderReview")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> SearchUnderReviewProperties([FromQuery] PaginatedRequest request)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var statuses = new[] { PropertyStatus.Pending };
            var result = await _propertyService.GetAgentPropertiesByStatusesAsync(agentId, request, statuses);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Search/Inactive")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> SearchInactiveProperties([FromQuery] PaginatedRequest request)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var statuses = new[] { PropertyStatus.Inactive, PropertyStatus.Withdrawn, PropertyStatus.Expired, PropertyStatus.Draft };
            var result = await _propertyService.GetAgentPropertiesByStatusesAsync(agentId, request, statuses);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Search")]
        public async Task<IActionResult> SearchProperties([FromBody] PropertySearchRequest request)
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (Guid?)null;

            var result = await _propertyService.SearchPropertiesAsync(request);

            // Track search if user is logged in
            if (userId.HasValue && result.Properties.Any())
            {
                _ = Task.Run(async () =>
                {
                    foreach (var property in result.Properties.Take(10))
                    {
                        await _recommendationService.TrackUserInteractionAsync(
                            userId.Value, property.Id, "search");
                    }
                });
            }

            return Ok(result);
        }

        [HttpGet("Published")]
        public async Task<IActionResult> GetPublishedProperties([FromQuery] PaginatedRequest request)
        {
            var result = await _propertyService.GetPublishedPropertiesAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Nearby")]
        public async Task<IActionResult> GetNearbyProperties(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] double radiusKm = 5)
        {
            var result = await _propertyService.GetNearbyPropertiesAsync(latitude, longitude, radiusKm);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Similar")]
        public async Task<IActionResult> GetSimilarProperties(
           [FromQuery] Guid propertyId,
            [FromQuery] int limit = 10)
        {
            var properties = await _propertyService.GetSimilarPropertiesAsync(propertyId, limit);
            return Ok(new { properties });
        }

        [HttpGet("Trending")]
        public async Task<IActionResult> GetTrendingProperties([FromQuery] int limit = 10)
        {
            var properties = await _propertyService.GetTrendingPropertiesAsync(limit);
            return Ok(new { properties });
        }

        [HttpGet("Recommendations")]
        [Authorize]
        public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var properties = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, limit);
            return Ok(new { properties });
        }

        //[HttpGet("Autocomplete")]
        //public async Task<IActionResult> Autocomplete([FromQuery] string query, [FromQuery] int limit = 10)
        //{
        //    var suggestions = await _elasticsearchService.AutocompleteAsync(query, limit);
        //    return Ok(new { suggestions });
        //}

        //[HttpGet("Locations/Suggest")]
        //public async Task<IActionResult> GetLocationSuggestions(
        //    [FromQuery] string query,
        //    [FromQuery] int limit = 10)
        //{
        //    //var locations = await _elasticsearchService.GetLocationSuggestionsAsync(query, limit);
        //    //return Ok(new { locations });
        //    return Ok(new {  });
        //}

        [HttpPost]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.CreatePropertyAsync(agentId, dto);

            if (result.Success)
                return StatusCode(result.StatusCode, result);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Draft")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> SaveDraft([FromBody] SaveAsDraftDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            dto.SaveAsDraft = true;
            var result = await _propertyService.SavePropertyAsDraftAsync(agentId, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("Draft")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpdateDraft([FromQuery] Guid propertyId, [FromBody] SaveAsDraftDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UpdatePropertyAsync(agentId, propertyId, dto);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Agent/Drafts")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> GetAgentDrafts([FromQuery] PaginatedRequest request)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var statuses = new[] { PropertyStatus.Draft };
            var result = await _propertyService.GetAgentPropertiesByStatusesAsync(agentId, request, statuses);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpdateProperty([FromQuery] Guid propertyId, [FromBody] SaveAsDraftDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UpdatePropertyAsync(agentId, propertyId, dto);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("")]
        public async Task<IActionResult> GetProperty([FromQuery] Guid propertyId)
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (Guid?)null;

            var result = await _propertyService.GetPropertyByIdAsync(propertyId, userId);

            // Increment view count asynchronously
            if (result.Success)
            {
                _ = _propertyService.IncrementViewCountAsync(propertyId);
            }

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Agent/Properties")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> GetAgentProperties(
            [FromQuery] PaginatedRequest request,
            [FromQuery] PropertyStatus? status = null)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.GetAgentPropertiesAsync(agentId, request, status);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("Delete")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> DeleteProperty([FromQuery] Guid propertyId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.DeletePropertyAsync(agentId, propertyId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Publish")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> PublishProperty([FromQuery] Guid propertyId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.PublishPropertyAsync(agentId, propertyId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Unpublish")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UnpublishProperty([FromQuery] Guid propertyId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UnpublishPropertyAsync(agentId, propertyId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("/Status")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpdatePropertyStatus(
            [FromQuery] Guid propertyId,
            [FromBody] PropertyStatus status)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UpdatePropertyStatusAsync(agentId, propertyId, status);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("Images/Primary")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> SetPrimaryImage([FromQuery] Guid propertyId, [FromQuery] Guid imageId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.SetPrimaryImageAsync(agentId, propertyId, imageId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("Amenities")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpsertPropertyAmenity(
            [FromQuery] Guid propertyId,
            [FromBody] PropertyAmenityDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UpsertPropertyAmenityAsync(
                agentId, propertyId, dto.Category, dto.Amenities);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Analytics")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> GetPropertyAnalytics([FromQuery] Guid propertyId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.GetPropertyAnalyticsAsync(agentId, propertyId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("Agent/Dashboard")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> GetAgentDashboard()
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.GetAgentDashboardAsync(agentId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Images")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UploadPropertyImage(
           [FromQuery] Guid propertyId,
            IFormFile image)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.AddPropertyImageAsync(agentId, propertyId, image);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("Images/Multiple")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UploadMultiplePropertyImages(
           [FromQuery] Guid propertyId,
            [FromForm] IFormFileCollection images)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.AddMultiplePropertyImagesAsync(agentId, propertyId, images);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("Images")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> DeletePropertyImage([FromQuery] Guid propertyId, [FromQuery] Guid imageId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.DeletePropertyImageAsync(agentId, propertyId, imageId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("SeedTestData")]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> SeedTestData()
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            // Create sample properties for testing
            var testProperties = new[]
            {
                new CreatePropertyDTO
                {
                    Title = "Luxury 3BR Apartment in Lekki Phase 1",
                    Description = "Modern apartment with pool, gym, and 24/7 security",
                    PropertyType = PropertyType.Apartment,
                    ListingType = ListingType.ForRent,
                    Tenor = PropertyTenor.Annually,
                    City = "Lagos",
                    State = "Lagos",
                    Country = "Nigeria",
                    Street = "Admiralty Way",
                    PostalCode = "101245",
                    Latitude = 6.4474m,
                    Longitude = 3.4700m,
                    Bedrooms = 3,
                    Bathrooms = 2.5m,
                    SquareFeet = 1500m,
                    Price = 5000000m,
                    Currency = "NGN"
                },
                // Add more test properties with varying prices, locations, etc.
            };

            foreach (var dto in testProperties)
            {
                await _propertyService.CreatePropertyAsync(agentId, dto);
            }

            return Ok(new { message = $"{testProperties.Length} test properties created" });
        }

        
        [HttpGet("TestCachePerformance")]
        public async Task<IActionResult> TestCachePerformance([FromQuery] string query)
        {
            var sw = Stopwatch.StartNew();

            // First call (no cache)
            var request = new PropertySearchRequest { Query = query, PageSize = 20 };
            var result1 = await _propertyService.SearchPropertiesAsync(request);
            var firstCallTime = sw.ElapsedMilliseconds;

            sw.Restart();

            // Second call (should be cached)
            var result2 = await _propertyService.SearchPropertiesAsync(request);
            var cachedCallTime = sw.ElapsedMilliseconds;

            return Ok(new
            {
                firstCall = $"{firstCallTime}ms",
                cachedCall = $"{cachedCallTime}ms",
                improvement = $"{((firstCallTime - cachedCallTime) / (double)firstCallTime * 100):F1}%",
                resultCount = result1.Properties.Count
            });
        }

    }
}