using AutoMapper;
using Habitera.DTOs;
using Habitera.Models;
using Habitera.Repositories;
using System.Text.Json;

namespace Habitera.Services
{
    public interface ISavedSearchService
    {
        Task<OperationResult<SavedSearch>> CreateSavedSearchAsync(Guid userId, PropertySearchRequest criteria, string name);
        Task<List<PropertyDTO>> GetNewMatchesAsync(Guid savedSearchId);
        Task SendAlertsAsync();
    }
    public class SavedSearchService : ISavedSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DatabaseSearchService _searchService;
        //private readonly IElasticsearchService _elasticsearchService;
        private readonly IRedisCacheService _cache;
        private readonly IEmailService _emailService;
        private readonly ILogger<SavedSearchService> _logger;

        public SavedSearchService(
           IUnitOfWork unitOfWork,
           DatabaseSearchService searchService,
           //IElasticsearchService elasticsearchService,
           IRedisCacheService cache,
           IEmailService emailService,
           ILogger<SavedSearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _searchService = searchService;
            //_elasticsearchService = elasticsearchService;
            _cache = cache;
            _emailService = emailService;
            _logger = logger;
        }


        public async Task<OperationResult<SavedSearch>> CreateSavedSearchAsync(
            Guid userId,
            PropertySearchRequest criteria,
            string name)
        {
            var savedSearch = new SavedSearch
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                SearchCriteria = JsonSerializer.Serialize(criteria),
                AlertsEnabled = true,
                Frequency = AlertFrequency.Daily,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavedSearches.AddAsync(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult<SavedSearch>.Successful(savedSearch);
        }

        public async Task<List<PropertyDTO>> GetNewMatchesAsync(Guid savedSearchId)
        {
            var savedSearch = await _unitOfWork.SavedSearches.GetByIdAsync(savedSearchId);
            if (savedSearch == null)
                return new List<PropertyDTO>();

            var criteria = JsonSerializer.Deserialize<PropertySearchRequest>(savedSearch.SearchCriteria);
            if (criteria == null)
                return new List<PropertyDTO>();

            // Get last check time from cache
            var lastCheckKey = $"savedsearch:{savedSearchId}:lastcheck";
            var lastCheck = await _cache.GetAsync<DateTime?>(lastCheckKey)
                            ?? savedSearch.LastAlertSent
                            ?? savedSearch.CreatedAt;

            // Add date filter to criteria
            criteria.PageSize = 100; // Get more results for alerts

            //var results = await _elasticsearchService.SearchPropertiesAsync(criteria);
            var results = await _searchService.SearchPropertiesAsync(criteria);

            // Filter to only new properties since last check
            var newMatches = results.Properties
                .Where(p => p.CreatedAt > lastCheck)
                .ToList();

            // Update last check time
            await _cache.SetAsync(lastCheckKey, DateTime.UtcNow, TimeSpan.FromDays(30));

            return newMatches;
        }

        public async Task SendAlertsAsync()
        {
            // Background job to send alerts
            var activeSearches = await _unitOfWork.SavedSearches
                .FindAsync(s => s.AlertsEnabled);

            foreach (var search in activeSearches)
            {
                try
                {
                    var newMatches = await GetNewMatchesAsync(search.Id);

                    if (newMatches.Any())
                    {
                        var user = await _unitOfWork.Users.GetByIdAsync(search.UserId);
                        if (user != null)
                        {
                            await _emailService.SendPropertyAlertsAsync(
                                user.Email!,
                                search.Name,
                                newMatches
                            );

                            search.LastAlertSent = DateTime.UtcNow;
                            _unitOfWork.SavedSearches.Update(search);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending alert for saved search {SearchId}", search.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
