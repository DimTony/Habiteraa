using Habitera.DTOs;
using Habitera.Models;
using Habitera.Repositories;
using System.Text;
using AutoMapper;

namespace Habitera.Services
{
    public interface IPropertyService
    {
        Task<OperationResult<PropertyDTO>> CreatePropertyAsync(Guid agentId, CreatePropertyDTO dto);
        Task<OperationResult<PropertyDTO>> UpdatePropertyAsync(Guid agentId, Guid propertyId, UpdatePropertyDTO dto);
        Task<OperationResult<PropertyDTO>> GetPropertyByIdAsync(Guid propertyId, Guid? userId = null);
        Task<OperationResult<string>> DeletePropertyAsync(Guid agentId, Guid propertyId);

        //// Agent Properties
        Task<PaginatedOperationResult<PropertyDTO>> GetAgentPropertiesAsync(
            Guid agentId, PaginatedRequest request, PropertyStatus? status = null);

        //// Publishing
        Task<OperationResult<string>> PublishPropertyAsync(Guid agentId, Guid propertyId);
        Task<OperationResult<string>> UnpublishPropertyAsync(Guid agentId, Guid propertyId);
        Task<OperationResult<string>> UpdatePropertyStatusAsync(Guid agentId, Guid propertyId, PropertyStatus status);

        // Images
        Task<OperationResult<PropertyImageDTO>> AddPropertyImageAsync(Guid agentId, Guid propertyId, IFormFile imageFile);
        Task<OperationResult<List<PropertyImageDTO>>> AddMultiplePropertyImagesAsync(Guid agentId, Guid propertyId, IFormFileCollection imageFiles);
        Task<OperationResult<string>> DeletePropertyImageAsync(Guid agentId, Guid propertyId, Guid imageId);
        Task<OperationResult<string>> SetPrimaryImageAsync(Guid agentId, Guid propertyId, Guid imageId);

        //// Amenities
        Task<OperationResult<PropertyAmenityDTO>> UpsertPropertyAmenityAsync(
            Guid agentId, Guid propertyId, AmenityCategory category, Dictionary<string, object> amenities);

        //// Analytics
        Task<OperationResult<PropertyAnalyticsDTO>> GetPropertyAnalyticsAsync(Guid agentId, Guid propertyId);
        Task<OperationResult<AgentDashboardDTO>> GetAgentDashboardAsync(Guid agentId);

        //// Public browsing
        Task<PaginatedOperationResult<PropertyDTO>> GetPublishedPropertiesAsync(PaginatedRequest request);
        Task<OperationResult<int>> IncrementViewCountAsync(Guid propertyId);
        Task<OperationResult<List<PropertyDTO>>> GetNearbyPropertiesAsync(decimal latitude, decimal longitude, double radiusKm);
        
        // Search
        Task<PropertySearchResponse> SearchPropertiesAsync(PropertySearchRequest request);
        Task<List<PropertyDTO>> GetTrendingPropertiesAsync(int limit = 10);
        Task<List<PropertyDTO>> GetSimilarPropertiesAsync(Guid propertyId, int limit = 10);
    }

    public class PropertyService : IPropertyService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DatabaseSearchService _searchService;
        //private readonly IElasticsearchService _elasticsearchService;
        //private readonly IPropertySyncService _syncService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IRedisCacheService _cache;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            DatabaseSearchService searchService,
            //IElasticsearchService elasticsearchService,
            //IPropertySyncService syncService,
            ICloudinaryService cloudinaryService,
            IRedisCacheService cache,
            ILogger<PropertyService> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _searchService = searchService;
            //_elasticsearchService = elasticsearchService;
            //_syncService = syncService;
            _cloudinaryService = cloudinaryService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<OperationResult<PropertyDTO>> CreatePropertyAsync(Guid agentId, CreatePropertyDTO dto)
        {
            try
            {
                // Validate agent
                var agent = await _unitOfWork.Users.GetByIdAsync(agentId);
                if (agent == null || agent.UserType != UserType.Agent)
                {
                    return OperationResult<PropertyDTO>.Failure("Agent not found", 404);
                }

                var property = new Property
                {
                    Id = Guid.NewGuid(),
                    AgentId = agentId,
                    Title = dto.Title,
                    Description = dto.Description,
                    PropertyType = dto.PropertyType,
                    ListingType = dto.ListingType,
                    Tenor = dto.Tenor,

                    Street = dto.Street,
                    City = dto.City,
                    State = dto.State,
                    Country = dto.Country,
                    PostalCode = dto.PostalCode,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,

                    Bedrooms = dto.Bedrooms,
                    Bathrooms = dto.Bathrooms,
                    SquareFeet = dto.SquareFeet,
                    LotSize = dto.LotSize,
                    YearBuilt = dto.YearBuilt,

                    Price = dto.Price,
                    Currency = dto.Currency,

                    Status = PropertyStatus.Active,
                    IsPublished = false,
                    IsFeatured = false,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Property created: {PropertyId} by Agent: {AgentId}",
                    property.Id, agentId);

                var propertyDto = _mapper.Map<PropertyDTO>(property);
                return OperationResult<PropertyDTO>.Successful(
                    propertyDto,
                    "Property created successfully",
                    201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property for agent {AgentId}", agentId);
                return OperationResult<PropertyDTO>.Failure(
                    "An error occurred while creating the property", 500);
            }
        }

        public async Task<OperationResult<PropertyDTO>> UpdatePropertyAsync(
            Guid agentId, Guid propertyId, UpdatePropertyDTO dto)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<PropertyDTO>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<PropertyDTO>.Failure("Unauthorized", 403);
                }

                // Update properties
                property.Title = dto.Title ?? property.Title;
                property.Description = dto.Description ?? property.Description;
                property.PropertyType = dto.PropertyType ?? property.PropertyType;
                property.ListingType = dto.ListingType ?? property.ListingType;
                property.Price = dto.Price ?? property.Price;
                property.Bedrooms = dto.Bedrooms ?? property.Bedrooms;
                property.Bathrooms = dto.Bathrooms ?? property.Bathrooms;
                property.SquareFeet = dto.SquareFeet ?? property.SquareFeet;
                property.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                // Sync to Elasticsearch if published
                //if (property.IsPublished)
                //{
                //    await _syncService.SyncPropertyToElasticsearchAsync(propertyId);
                //}

                var propertyDto = _mapper.Map<PropertyDTO>(property);
                return OperationResult<PropertyDTO>.Successful(
                    propertyDto,
                    "Property updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property {PropertyId}", propertyId);
                return OperationResult<PropertyDTO>.Failure(
                    "An error occurred while updating the property", 500);
            }
        }

        public async Task<OperationResult<PropertyDTO>> GetPropertyByIdAsync(
            Guid propertyId, Guid? userId = null)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetPropertyWithAllDetailsAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<PropertyDTO>.Failure("Property not found", 404);
                }

                // Check if property is published (unless it's the agent viewing their own)
                if (!property.IsPublished && (!userId.HasValue || property.AgentId != userId.Value))
                {
                    return OperationResult<PropertyDTO>.Failure("Property not found", 404);
                }

                var propertyDto = _mapper.Map<PropertyDTO>(property);

                // Add favorite status if user is logged in
                if (userId.HasValue)
                {
                    propertyDto.IsFavorited = await _unitOfWork.Favorites
                        .IsFavoriteAsync(userId.Value, propertyId);
                }

                return OperationResult<PropertyDTO>.Successful(propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting property {PropertyId}", propertyId);
                return OperationResult<PropertyDTO>.Failure(
                    "An error occurred while retrieving the property", 500);
            }
        }
        public async Task<OperationResult<string>> DeletePropertyAsync(Guid agentId, Guid propertyId)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<string>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                // Soft delete by changing status
                property.Status = PropertyStatus.Deleted;
                property.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Properties.Update(property);

                await _unitOfWork.SaveChangesAsync();

                // Remove from Elasticsearch
                //await _elasticsearchService.DeletePropertyAsync(propertyId);

                _logger.LogInformation("Property {PropertyId} deleted by Agent {AgentId}",
                    propertyId, agentId);

                return OperationResult<string>.Successful(
                    "Property deleted successfully",
                    "Property deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting property {PropertyId}", propertyId);
                return OperationResult<string>.Failure(
                    "An error occurred while deleting the property", 500);
            }
        }
        public async Task<PaginatedOperationResult<PropertyDTO>> GetAgentPropertiesAsync(
            Guid agentId,
            PaginatedRequest request,
            PropertyStatus? status = null)
        {
            try
            {
                var result = await _unitOfWork.Properties.GetPropertiesByAgentPagedAsync(
                    agentId,
                    request.PageNumber,
                    request.PageSize,
                    status);

                var propertyDtos = _mapper.Map<List<PropertyDTO>>(result.Items);

                return PaginatedOperationResult<PropertyDTO>.Successful(
                    propertyDtos,
                    result.TotalCount,
                    request.PageNumber,
                    request.PageSize,
                    "Properties retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties for agent {AgentId}", agentId);
                return PaginatedOperationResult<PropertyDTO>.Failure(
                    "An error occurred while retrieving properties", 500);
            }
        }

        public async Task<OperationResult<string>> PublishPropertyAsync(Guid agentId, Guid propertyId)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetPropertyWithAllDetailsAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<string>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                if (property.IsPublished)
                {
                    return OperationResult<string>.Failure("Property is already published", 400);
                }

                // Validate property has minimum required data
                if (string.IsNullOrWhiteSpace(property.Title))
                {
                    return OperationResult<string>.Failure(
                        "Property must have a title before publishing", 400);
                }

                if (!property.Images.Any())
                {
                    return OperationResult<string>.Failure(
                        "Property must have at least one image before publishing", 400);
                }

                await _unitOfWork.Properties.PublishPropertyAsync(propertyId);

                // Index in Elasticsearch
                //await _elasticsearchService.IndexPropertyAsync(property);

                _logger.LogInformation("Property {PropertyId} published by Agent {AgentId}",
                    propertyId, agentId);

                return OperationResult<string>.Successful(
                    "Property published successfully",
                    "Property published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing property {PropertyId}", propertyId);
                return OperationResult<string>.Failure(
                    "An error occurred while publishing the property", 500);
            }
        }

        public async Task<OperationResult<string>> UnpublishPropertyAsync(Guid agentId, Guid propertyId)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<string>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                if (!property.IsPublished)
                {
                    return OperationResult<string>.Failure("Property is already unpublished", 400);
                }

                await _unitOfWork.Properties.UnpublishPropertyAsync(propertyId);

                // Remove from Elasticsearch
                //await _elasticsearchService.DeletePropertyAsync(propertyId);

                _logger.LogInformation("Property {PropertyId} unpublished by Agent {AgentId}",
                    propertyId, agentId);

                return OperationResult<string>.Successful(
                    "Property unpublished successfully",
                    "Property unpublished successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing property {PropertyId}", propertyId);
                return OperationResult<string>.Failure(
                    "An error occurred while unpublishing the property", 500);
            }
        }

        public async Task<OperationResult<string>> UpdatePropertyStatusAsync(
            Guid agentId,
            Guid propertyId,
            PropertyStatus status)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<string>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                await _unitOfWork.Properties.UpdatePropertyStatusAsync(propertyId, status);

                // Update in Elasticsearch if published
                //if (property.IsPublished)
                //{
                //    await _elasticsearchService.UpdatePropertyStatusAsync(propertyId, status);
                //}

                _logger.LogInformation(
                    "Property {PropertyId} status updated to {Status} by Agent {AgentId}",
                    propertyId, status, agentId);

                return OperationResult<string>.Successful(
                    $"Property status updated to {status}",
                    $"Property status updated to {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property status for {PropertyId}", propertyId);
                return OperationResult<string>.Failure(
                    "An error occurred while updating property status", 500);
            }
        }

        public async Task<OperationResult<string>> SetPrimaryImageAsync(
            Guid agentId,
            Guid propertyId,
            Guid imageId)
        {
            try
            {
                var isOwner = await _unitOfWork.Properties.IsAgentOwnerAsync(propertyId, agentId);
                if (!isOwner)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                var image = await _unitOfWork.PropertyImages
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == propertyId);

                if (image == null)
                {
                    return OperationResult<string>.Failure("Image not found", 404);
                }

                await _unitOfWork.PropertyImages.SetPrimaryImageAsync(propertyId, imageId);

                // Sync to Elasticsearch
                //await _syncService.SyncPropertyToElasticsearchAsync(propertyId);

                return OperationResult<string>.Successful(
                    "Primary image updated successfully",
                    "Primary image updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary image for property {PropertyId}", propertyId);
                return OperationResult<string>.Failure(
                    "An error occurred while setting primary image", 500);
            }
        }

        public async Task<OperationResult<PropertyAmenityDTO>> UpsertPropertyAmenityAsync(
            Guid agentId,
            Guid propertyId,
            AmenityCategory category,
            Dictionary<string, object> amenities)
        {
            try
            {
                var isOwner = await _unitOfWork.Properties.IsAgentOwnerAsync(propertyId, agentId);
                if (!isOwner)
                {
                    return OperationResult<PropertyAmenityDTO>.Failure("Unauthorized", 403);
                }

                await _unitOfWork.PropertyAmenities.UpsertAmenityAsync(
                    propertyId,
                    category,
                    amenities);

                // Sync to Elasticsearch
                //await _syncService.SyncPropertyToElasticsearchAsync(propertyId);

                var amenityDto = new PropertyAmenityDTO
                {
                    Category = category,
                    Amenities = amenities
                };

                return OperationResult<PropertyAmenityDTO>.Successful(
                    amenityDto,
                    "Amenities updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating amenities for property {PropertyId}", propertyId);
                return OperationResult<PropertyAmenityDTO>.Failure(
                    "An error occurred while updating amenities", 500);
            }
        }

        public async Task<OperationResult<PropertyAnalyticsDTO>> GetPropertyAnalyticsAsync(
            Guid agentId,
            Guid propertyId)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetPropertyWithAllDetailsAsync(propertyId);

                if (property == null)
                {
                    return OperationResult<PropertyAnalyticsDTO>.Failure("Property not found", 404);
                }

                if (property.AgentId != agentId)
                {
                    return OperationResult<PropertyAnalyticsDTO>.Failure("Unauthorized", 403);
                }

                var bookings = await _unitOfWork.ViewingBookings.GetPropertyBookingsAsync(propertyId);

                var daysOnMarket = property.PublishedAt.HasValue
                    ? (DateTime.UtcNow - property.PublishedAt.Value).Days
                    : 0;

                var analytics = new PropertyAnalyticsDTO
                {
                    PropertyId = propertyId,
                    TotalViews = property.ViewCount,
                    TotalFavorites = property.FavoriteCount,
                    TotalBookings = bookings.Count(),
                    DaysOnMarket = daysOnMarket,
                    RecentBookings = _mapper.Map<List<BookingDTO>>(
                        bookings.OrderByDescending(b => b.CreatedAt).Take(5))
                };

                return OperationResult<PropertyAnalyticsDTO>.Successful(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics for property {PropertyId}", propertyId);
                return OperationResult<PropertyAnalyticsDTO>.Failure(
                    "An error occurred while retrieving property analytics", 500);
            }
        }

        public async Task<OperationResult<AgentDashboardDTO>> GetAgentDashboardAsync(Guid agentId)
        {
            try
            {
                var statusBreakdown = await _unitOfWork.Properties
                    .GetPropertyCountByStatusAsync(agentId);

                var recentProperties = await _unitOfWork.Properties
                    .GetPropertiesByAgentPagedAsync(agentId, 1, 5);

                var dashboard = new AgentDashboardDTO
                {
                    TotalProperties = statusBreakdown.Values.Sum(),
                    ActiveProperties = statusBreakdown.GetValueOrDefault(PropertyStatus.Active, 0),
                    PendingProperties = statusBreakdown.GetValueOrDefault(PropertyStatus.Pending, 0),
                    SoldProperties = statusBreakdown.GetValueOrDefault(PropertyStatus.Sold, 0),
                    RecentProperties = _mapper.Map<List<PropertyDTO>>(recentProperties.Items),
                    PropertyStatusBreakdown = statusBreakdown
                };

                return OperationResult<AgentDashboardDTO>.Successful(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard for agent {AgentId}", agentId);
                return OperationResult<AgentDashboardDTO>.Failure(
                    "An error occurred while retrieving dashboard data", 500);
            }
        }

        public async Task<PaginatedOperationResult<PropertyDTO>> GetPublishedPropertiesAsync(
            PaginatedRequest request)
        {
            try
            {
                var searchRequest = new PropertySearchRequest
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Query = request.SearchWord,
                    Statuses = new List<string> { PropertyStatus.Active.ToString() },
                    SortBy = "newest"
                };

                //var results = await _elasticsearchService.SearchPropertiesAsync(searchRequest);
                var results = await _searchService.SearchPropertiesAsync(searchRequest);

                return PaginatedOperationResult<PropertyDTO>.Successful(
                    results.Properties,
                    (int)results.TotalCount,
                    request.PageNumber,
                    request.PageSize,
                    "Properties retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting published properties");
                return PaginatedOperationResult<PropertyDTO>.Failure(
                    "An error occurred while retrieving properties", 500);
            }
        }

        public async Task<OperationResult<List<PropertyDTO>>> GetNearbyPropertiesAsync(
             decimal latitude, decimal longitude, double radiusKm)
        {
            try
            {
                var properties = await _searchService.SearchByRadiusAsync(
                    latitude, longitude, radiusKm);

                return OperationResult<List<PropertyDTO>>.Successful(
                    properties,
                    $"Found {properties.Count} properties within {radiusKm}km");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting nearby properties");
                return OperationResult<List<PropertyDTO>>.Failure(
                    "An error occurred while retrieving nearby properties", 500);
            }
        }

        public async Task<List<PropertyDTO>> GetSimilarPropertiesAsync(Guid propertyId, int limit = 10)
        {
            try
            {
                var cacheKey = $"similar:properties:{propertyId}:{limit}";

                var cached = await _cache.GetAsync<List<PropertyDTO>>(cacheKey);
                if (cached != null)
                    return cached;

                return new List<PropertyDTO>();

                //var similar = await _elasticsearchService.GetSimilarPropertiesAsync(propertyId, limit);

                //// Cache for 1 hour
                //await _cache.SetAsync(cacheKey, similar, TimeSpan.FromHours(1));

                //return similar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar properties for {PropertyId}", propertyId);
                return new List<PropertyDTO>();
            }
        }
        public async Task<OperationResult<int>> IncrementViewCountAsync(Guid propertyId)
        {
            try
            {
                var newCount = await _unitOfWork.Properties.IncrementViewCountAsync(propertyId);

                // Update in Elasticsearch asynchronously
                //_ = Task.Run(async () =>
                //{
                //    try
                //    {
                //        await _syncService.SyncPropertyToElasticsearchAsync(propertyId);
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.LogError(ex, "Failed to sync view count to Elasticsearch");
                //    }
                //});

                return OperationResult<int>.Successful(newCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing view count for {PropertyId}", propertyId);
                return OperationResult<int>.Failure("Failed to increment view count", 500);
            }
        }

        public async Task<PropertySearchResponse> SearchPropertiesAsync(PropertySearchRequest request)
        {
            try
            {
                var cacheKey = $"search:{GetSearchCacheKey(request)}";
                var cached = await _cache.GetAsync<PropertySearchResponse>(cacheKey);

                if (cached != null)
                    return cached;

                var results = await _searchService.SearchPropertiesAsync(request);
                await _cache.SetAsync(cacheKey, results, TimeSpan.FromMinutes(5));

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching properties");
                return new PropertySearchResponse();
            }
        }

        public async Task<List<PropertyDTO>> GetTrendingPropertiesAsync(int limit = 10)
        {
            var cacheKey = $"trending:properties:{limit}";

            var cached = await _cache.GetAsync<List<PropertyDTO>>(cacheKey);
            if (cached != null)
                return cached;

            // Get from Elasticsearch - sorted by views in last 7 days
            var searchRequest = new PropertySearchRequest
            {
                PageSize = limit,
                SortBy = "popular",
                // Add date filter for last 7 days
            };

            //var results = await _elasticsearchService.SearchPropertiesAsync(searchRequest);
            var results = await _searchService.SearchPropertiesAsync(searchRequest);
            //var dtos = results.Properties.Select(MapToDTO).ToList();
            var dtos = results.Properties.ToList();

            // Cache for 30 minutes
            await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(30));

            return dtos;
        }

        public async Task<OperationResult<PropertyImageDTO>> AddPropertyImageAsync(
        Guid agentId,
        Guid propertyId,
        IFormFile imageFile)
        {
            try
            {
                // Check property ownership
                var isOwner = await _unitOfWork.Properties.IsAgentOwnerAsync(propertyId, agentId);
                if (!isOwner)
                {
                    return OperationResult<PropertyImageDTO>.Failure("Unauthorized", 403);
                }

                // Upload to Cloudinary
                var uploadResult = await _cloudinaryService.UploadImageWithThumbnailAsync(
                    imageFile,
                    $"properties/{propertyId}");

                if (!uploadResult.Success || uploadResult.Data == null)
                {
                    return OperationResult<PropertyImageDTO>.Failure(
                        uploadResult.Message,
                        uploadResult.StatusCode);
                }

                // Get current image count for display order
                var existingImages = await _unitOfWork.PropertyImages
                    .GetImagesByPropertyIdAsync(propertyId);

                var displayOrder = existingImages.Any()
                    ? existingImages.Max(i => i.DisplayOrder) + 1
                    : 0;

                // Save to database
                var propertyImage = new PropertyImage
                {
                    Id = Guid.NewGuid(),
                    PropertyId = propertyId,
                    ImageUrl = uploadResult.Data.Url,
                    ThumbnailUrl = uploadResult.Data.ThumbnailUrl,
                    DisplayOrder = displayOrder,
                    IsPrimary = !existingImages.Any(), // First image is primary
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PropertyImages.AddAsync(propertyImage);
                await _unitOfWork.SaveChangesAsync();

                // Sync to Elasticsearch
                //await _syncService.SyncPropertyToElasticsearchAsync(propertyId);

                var imageDto = _mapper.Map<PropertyImageDTO>(propertyImage);
                return OperationResult<PropertyImageDTO>.Successful(
                    imageDto,
                    "Image uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding image to property {PropertyId}", propertyId);
                return OperationResult<PropertyImageDTO>.Failure(
                    "Failed to upload image", 500);
            }
        }

        public async Task<OperationResult<List<PropertyImageDTO>>> AddMultiplePropertyImagesAsync(
            Guid agentId,
            Guid propertyId,
            IFormFileCollection imageFiles)
        {
            try
            {
                var isOwner = await _unitOfWork.Properties.IsAgentOwnerAsync(propertyId, agentId);
                if (!isOwner)
                {
                    return OperationResult<List<PropertyImageDTO>>.Failure("Unauthorized", 403);
                }

                // Upload all images to Cloudinary
                var uploadResults = await _cloudinaryService.UploadMultipleImagesAsync(
                    imageFiles,
                    $"properties/{propertyId}");

                if (!uploadResults.Success || uploadResults.Data == null)
                {
                    return OperationResult<List<PropertyImageDTO>>.Failure(
                        uploadResults.Message,
                        uploadResults.StatusCode);
                }

                var existingImages = await _unitOfWork.PropertyImages
                    .GetImagesByPropertyIdAsync(propertyId);

                var startDisplayOrder = existingImages.Any()
                    ? existingImages.Max(i => i.DisplayOrder) + 1
                    : 0;

                var propertyImages = new List<PropertyImage>();

                for (int i = 0; i < uploadResults.Data.Count; i++)
                {
                    var uploadData = uploadResults.Data[i];

                    propertyImages.Add(new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = propertyId,
                        ImageUrl = uploadData.Url,
                        ThumbnailUrl = uploadData.ThumbnailUrl,
                        DisplayOrder = startDisplayOrder + i,
                        IsPrimary = !existingImages.Any() && i == 0,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _unitOfWork.PropertyImages.AddRangeAsync(propertyImages);
                await _unitOfWork.SaveChangesAsync();

                //await _syncService.SyncPropertyToElasticsearchAsync(propertyId);

                var imageDtos = _mapper.Map<List<PropertyImageDTO>>(propertyImages);
                return OperationResult<List<PropertyImageDTO>>.Successful(
                    imageDtos,
                    $"{propertyImages.Count} images uploaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple images to property {PropertyId}", propertyId);
                return OperationResult<List<PropertyImageDTO>>.Failure(
                    "Failed to upload images", 500);
            }
        }

        public async Task<OperationResult<string>> DeletePropertyImageAsync(
            Guid agentId,
            Guid propertyId,
            Guid imageId)
        {
            try
            {
                var isOwner = await _unitOfWork.Properties.IsAgentOwnerAsync(propertyId, agentId);
                if (!isOwner)
                {
                    return OperationResult<string>.Failure("Unauthorized", 403);
                }

                var image = await _unitOfWork.PropertyImages
                    .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == propertyId);

                if (image == null)
                {
                    return OperationResult<string>.Failure("Image not found", 404);
                }

                // Extract public ID from Cloudinary URL
                var publicId = ExtractPublicIdFromUrl(image.ImageUrl);

                if (!string.IsNullOrEmpty(publicId))
                {
                    // Delete from Cloudinary
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }

                // Delete from database
                _unitOfWork.PropertyImages.Remove(image);
                await _unitOfWork.SaveChangesAsync();

                // If it was primary, set another image as primary
                if (image.IsPrimary)
                {
                    var remainingImages = await _unitOfWork.PropertyImages
                        .GetImagesByPropertyIdAsync(propertyId);

                    if (remainingImages.Any())
                    {
                        var newPrimary = remainingImages.First();
                        await _unitOfWork.PropertyImages.SetPrimaryImageAsync(propertyId, newPrimary.Id);
                    }
                }

                //await _syncService.SyncPropertyToElasticsearchAsync(propertyId);

                return OperationResult<string>.Successful(
                    "Image deleted successfully",
                    "Image deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
                return OperationResult<string>.Failure("Failed to delete image", 500);
            }
        }

        private string? ExtractPublicIdFromUrl(string imageUrl)
        {
            try
            {
                // Example URL: https://res.cloudinary.com/demo/image/upload/v1234/properties/abc123.jpg
                // Public ID: properties/abc123

                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');

                // Find "upload" segment and take everything after version number
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex >= 0 && uploadIndex + 2 < segments.Length)
                {
                    var pathAfterVersion = segments.Skip(uploadIndex + 2).ToArray();
                    var publicIdWithExtension = string.Join("/", pathAfterVersion);

                    // Remove file extension
                    return Path.ChangeExtension(publicIdWithExtension, null);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private string GetSearchCacheKey(PropertySearchRequest request)
        {
            var key = $"{request.Query}:{request.City}:{request.State}:" +
                      $"{request.MinPrice}:{request.MaxPrice}:{request.MinBedrooms}:" +
                      $"{request.PropertyTypes}:{request.PageNumber}:{request.PageSize}";

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
        }

        private PropertyDTO MapToDTO(Property doc)
        {
            // Your mapping logic
            return new PropertyDTO();
        }
    }
}
