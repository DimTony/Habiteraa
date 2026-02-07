using Habitera.Models;
using Habitera.Repositories;
using Habitera.Services;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Habitera.Extensions
{
    public static class RepositoryServiceExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IPropertyRepository, PropertyRepository>();
            services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
            services.AddScoped<IPropertyAmenityRepository, PropertyAmenityRepository>();
            services.AddScoped<IFavoriteRepository, FavoriteRepository>();
            services.AddScoped<IViewingBookingRepository, ViewingBookingRepository>();

            return services;
        }
    }

    public static class ElasticsearchServiceExtensions
    {
        public static IServiceCollection AddElasticsearch(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var settings = configuration.GetSection("Elasticsearch").Get<ElasticsearchSettings>()
                ?? new ElasticsearchSettings();

            var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Uri));

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                clientSettings.Authentication(new ApiKey(settings.ApiKey));
            }
            else if (!string.IsNullOrWhiteSpace(settings.Username) &&
                     !string.IsNullOrWhiteSpace(settings.Password))
            {
                clientSettings.Authentication(new BasicAuthentication(settings.Username, settings.Password));
            }

            if (settings.EnableDebugMode)
            {
                clientSettings.EnableDebugMode();
            }

            clientSettings.ServerCertificateValidationCallback(
                CertificateValidations.AllowAll);

            var client = new ElasticsearchClient(clientSettings);

            services.AddSingleton(client);
            services.AddScoped<IElasticsearchService, ElasticsearchService>();

            return services;
        }
    }

}
