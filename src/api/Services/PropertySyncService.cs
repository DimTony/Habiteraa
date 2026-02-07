using Habitera.Models;
using Habitera.Repositories;

namespace Habitera.Services
{
    public interface IPropertySyncService
    {
        Task SyncPropertyToElasticsearchAsync(Guid propertyId);
        Task SyncPropertiesInBatchAsync(IEnumerable<Guid> propertyIds);
        Task HandlePropertyCreatedAsync(Property property);
        Task HandlePropertyUpdatedAsync(Property property);
        Task HandlePropertyDeletedAsync(Guid propertyId);
    }

    public class PropertySyncService : IPropertySyncService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<PropertySyncService> _logger;

        public PropertySyncService(
            IUnitOfWork unitOfWork,
            IElasticsearchService elasticsearchService,
            ILogger<PropertySyncService> logger)
        {
            _unitOfWork = unitOfWork;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        public async Task SyncPropertyToElasticsearchAsync(Guid propertyId)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetPropertyWithAllDetailsAsync(propertyId);

                if (property == null)
                {
                    _logger.LogWarning("Property {PropertyId} not found for sync", propertyId);
                    await _elasticsearchService.DeletePropertyAsync(propertyId);
                    return;
                }

                if (property.IsPublished)
                {
                    await _elasticsearchService.UpdatePropertyAsync(propertyId, property);
                    _logger.LogInformation("Synced property {PropertyId} to Elasticsearch", propertyId);
                }
                else
                {
                    await _elasticsearchService.DeletePropertyAsync(propertyId);
                    _logger.LogInformation("Removed unpublished property {PropertyId} from Elasticsearch", propertyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing property {PropertyId}", propertyId);
                throw;
            }
        }

        public async Task SyncPropertiesInBatchAsync(IEnumerable<Guid> propertyIds)
        {
            var properties = await _unitOfWork.Properties.GetPropertiesByIdsAsync(propertyIds);

            var publishedProperties = properties.Where(p => p.IsPublished).ToList();

            if (publishedProperties.Any())
            {
                await _elasticsearchService.BulkIndexPropertiesAsync(publishedProperties);
            }
        }

        public async Task HandlePropertyCreatedAsync(Property property)
        {
            if (property.IsPublished)
            {
                await _elasticsearchService.IndexPropertyAsync(property);
                _logger.LogInformation("Indexed new property {PropertyId}", property.Id);
            }
        }

        public async Task HandlePropertyUpdatedAsync(Property property)
        {
            await SyncPropertyToElasticsearchAsync(property.Id);
        }

        public async Task HandlePropertyDeletedAsync(Guid propertyId)
        {
            await _elasticsearchService.DeletePropertyAsync(propertyId);
            _logger.LogInformation("Deleted property {PropertyId} from Elasticsearch", propertyId);
        }
    }
}