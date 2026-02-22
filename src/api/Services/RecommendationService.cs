using Habitera.DTOs;
using Habitera.Models;
using Habitera.Repositories;

namespace Habitera.Services
{
    public interface IRecommendationService
    {
        Task<List<PropertyDTO>> GetPersonalizedRecommendationsAsync(Guid userId, int limit = 10);
        Task TrackUserInteractionAsync(Guid userId, Guid propertyId, string interactionType);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly IRedisCacheService _cache;
        private readonly DatabaseSearchService _searchService;
        //private readonly IElasticsearchService _elasticsearchService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IRedisCacheService cache,
            DatabaseSearchService searchService,
            //IElasticsearchService elasticsearchService,
            IUnitOfWork unitOfWork,
            ILogger<RecommendationService> logger)
        {
            _cache = cache;
            _searchService = searchService;
            //_elasticsearchService = elasticsearchService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<PropertyDTO>> GetPersonalizedRecommendationsAsync(
            Guid userId,
            int limit = 10)
        {
            try
            {
                // Check cache first
                var cacheKey = $"recommendations:{userId}:{limit}";
                var cached = await _cache.GetAsync<List<PropertyDTO>>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                // Get user's interaction history
                var preferences = await GetUserPreferencesAsync(userId);

                if (preferences == null)
                {
                    // New user - return trending/featured properties
                    //var trending = await _elasticsearchService.GetRecommendedPropertiesAsync(
                    //    userId.ToString(),
                    //    limit
                    //);
                    //var trending = await _searchService.GetRecommendedPropertiesAsync(
                    //    userId.ToString(),
                    //    limit
                    //);

                    //// Cache for 1 hour
                    //await _cache.SetAsync(cacheKey, trending, TimeSpan.FromHours(1));
                    //return trending;
                    return new List<PropertyDTO>();
                }

                // Build search based on preferences
                var searchRequest = BuildRecommendationQuery(preferences, limit * 3); // Get more for filtering
                //var results = await _elasticsearchService.SearchPropertiesAsync(searchRequest);
                var results = await _searchService.SearchPropertiesAsync(searchRequest);

                // Score and rank results
                var scoredResults = await ScoreRecommendationsAsync(userId, results.Properties, preferences);

                // Get top N
                var recommendations = scoredResults
                    .OrderByDescending(r => r.Score)
                    .Take(limit)
                    .Select(r => r.Property)
                    .ToList();

                // Cache for 30 minutes
                await _cache.SetAsync(cacheKey, recommendations, TimeSpan.FromMinutes(30));

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
                return new List<PropertyDTO>();
            }
        }

        public async Task TrackUserInteractionAsync(
            Guid userId,
            Guid propertyId,
            string interactionType)
        {
            try
            {
                var key = $"user:{userId}:{interactionType.ToLower()}";
                var interactions = await _cache.GetAsync<List<Guid>>(key) ?? new List<Guid>();

                if (!interactions.Contains(propertyId))
                {
                    interactions.Insert(0, propertyId);

                    // Keep last 100 interactions
                    if (interactions.Count > 100)
                        interactions = interactions.Take(100).ToList();

                    await _cache.SetAsync(key, interactions, TimeSpan.FromDays(90));
                }

                // Invalidate recommendation cache
                await _cache.RemoveByPatternAsync($"recommendations:{userId}:*");

                // Store in database for analytics (async)
                _ = Task.Run(async () =>
                {
                    var interaction = new UserInteraction
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        PropertyId = propertyId,
                        Type = Enum.Parse<InteractionType>(interactionType, true),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserInteractions.AddAsync(interaction);
                    await _unitOfWork.SaveChangesAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking interaction for user {UserId}", userId);
            }
        }

        private async Task<UserPreferences?> GetUserPreferencesAsync(Guid userId)
        {
            // Try cache first
            var cacheKey = $"userpreferences:{userId}";
            var cached = await _cache.GetAsync<UserPreferences>(cacheKey);
            if (cached != null)
                return cached;

            // Get user's favorited and viewed properties
            var favoriteKey = $"user:{userId}:favorited";
            var viewedKey = $"user:{userId}:viewed";

            var favoritedIds = await _cache.GetAsync<List<Guid>>(favoriteKey) ?? new List<Guid>();
            var viewedIds = await _cache.GetAsync<List<Guid>>(viewedKey) ?? new List<Guid>();

            // Combine and get unique IDs (favor favorites over views)
            var interactedIds = favoritedIds.Union(viewedIds).Take(50).ToList();

            if (!interactedIds.Any())
                return null;

            // Get properties from database
            var properties = await _unitOfWork.Properties.GetPropertiesByIdsAsync(interactedIds);
            var propList = properties.ToList();

            if (!propList.Any())
                return null;

            // Calculate preferences
            var preferences = new UserPreferences
            {
                AveragePriceRange = new PriceRange
                {
                    Min = propList.Min(p => p.Price) * 0.7m,
                    Max = propList.Max(p => p.Price) * 1.3m
                },
                PreferredBedroomCount = (int)Math.Round(propList.Average(p => p.Bedrooms)),
                PreferredBathroomCount = (int)Math.Round(propList.Average(p => (double)p.Bathrooms)),
                PreferredSquareFeet = propList.Average(p => p.SquareFeet),
                PreferredPropertyTypes = propList
                    .GroupBy(p => p.PropertyType)
                    .OrderByDescending(g => g.Count())
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList(),
                PreferredCities = propList
                    .GroupBy(p => p.City)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList(),
                PreferredStates = propList
                    .GroupBy(p => p.State)
                    .OrderByDescending(g => g.Count())
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList(),
                CalculatedAt = DateTime.UtcNow
            };

            // Cache for 24 hours
            await _cache.SetAsync(cacheKey, preferences, TimeSpan.FromHours(24));

            return preferences;
        }

        private PropertySearchRequest BuildRecommendationQuery(
            UserPreferences preferences,
            int limit)
        {
            return new PropertySearchRequest
            {
                MinPrice = preferences.AveragePriceRange.Min,
                MaxPrice = preferences.AveragePriceRange.Max,
                MinBedrooms = Math.Max(1, preferences.PreferredBedroomCount - 1),
                MaxBedrooms = preferences.PreferredBedroomCount + 2,
                PropertyTypes = preferences.PreferredPropertyTypes
                    .Select(pt => pt.ToString())
                    .ToList(),
                Statuses = new List<string> { PropertyStatus.Active.ToString() },
                PageSize = limit,
                SortBy = "relevance"
            };
        }

        private async Task<List<RecommendationScore>> ScoreRecommendationsAsync(
            Guid userId,
            IEnumerable<PropertyDTO> properties,
            UserPreferences preferences)
        {
            var viewedKey = $"user:{userId}:viewed";
            var viewedIds = await _cache.GetAsync<List<Guid>>(viewedKey) ?? new List<Guid>();

            var scored = new List<RecommendationScore>();

            foreach (var property in properties)
            {
                // Skip already viewed
                if (viewedIds.Contains(property.Id))
                    continue;

                var score = 0.0;
                var factors = new Dictionary<string, double>();

                // Price match (30% weight)
                var priceScore = CalculatePriceScore(property.Price, preferences.AveragePriceRange);
                factors["price"] = priceScore;
                score += priceScore * 0.3;

                // Property type match (20% weight)
                var typeScore = preferences.PreferredPropertyTypes.Contains(
                    Enum.Parse<PropertyType>(property.PropertyType.ToString())) ? 1.0 : 0.5;
                factors["propertyType"] = typeScore;
                score += typeScore * 0.2;

                // Location match (20% weight)
                var locationScore = preferences.PreferredCities.Contains(property.City) ? 1.0 : 0.3;
                factors["location"] = locationScore;
                score += locationScore * 0.2;

                // Bedrooms match (15% weight)
                var bedroomDiff = Math.Abs(property.Bedrooms - preferences.PreferredBedroomCount);
                var bedroomScore = Math.Max(0, 1.0 - (bedroomDiff * 0.25));
                factors["bedrooms"] = bedroomScore;
                score += bedroomScore * 0.15;

                // Recency boost (10% weight)
                var daysOld = (DateTime.UtcNow - property.CreatedAt).Days;
                var recencyScore = Math.Max(0, 1.0 - (daysOld / 30.0));
                factors["recency"] = recencyScore;
                score += recencyScore * 0.1;

                // Featured boost (5% weight)
                if (property.IsFeatured)
                {
                    factors["featured"] = 1.0;
                    score += 0.05;
                }

                scored.Add(new RecommendationScore
                {
                    PropertyId = property.Id,
                    Property = property,
                    Score = score,
                    FactorScores = factors,
                    Reason = GenerateReason(factors, preferences)
                });
            }

            return scored;
        }

        private double CalculatePriceScore(decimal price, PriceRange range)
        {
            if (price < range.Min || price > range.Max)
                return 0.0;

            var midpoint = (range.Min + range.Max) / 2;
            var deviation = Math.Abs(price - midpoint);
            var maxDeviation = (range.Max - range.Min) / 2;

            return 1.0 - ((double)deviation / (double)maxDeviation);
        }

        private string GenerateReason(
            Dictionary<string, double> factors,
            UserPreferences preferences)
        {
            var topFactors = factors
                .OrderByDescending(f => f.Value)
                .Take(2)
                .ToList();

            var reasons = new List<string>();

            foreach (var factor in topFactors)
            {
                switch (factor.Key)
                {
                    case "price":
                        reasons.Add("matches your budget");
                        break;
                    case "propertyType":
                        reasons.Add("similar to properties you liked");
                        break;
                    case "location":
                        reasons.Add($"in {preferences.PreferredCities.First()}");
                        break;
                    case "bedrooms":
                        reasons.Add($"{preferences.PreferredBedroomCount} bedrooms");
                        break;
                    case "recency":
                        reasons.Add("newly listed");
                        break;
                    case "featured":
                        reasons.Add("featured property");
                        break;
                }
            }

            return string.Join(", ", reasons);
        }
    }

}
