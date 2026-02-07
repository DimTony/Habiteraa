using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Habitera.Services
{
    public class ElasticsearchSyncJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ElasticsearchSyncJob> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(1);

        public ElasticsearchSyncJob(
            IServiceProvider serviceProvider,
            ILogger<ElasticsearchSyncJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Elasticsearch Sync Job started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var elasticsearchService = scope.ServiceProvider
                        .GetRequiredService<IElasticsearchService>();

                    _logger.LogInformation("Running Elasticsearch sync check");

                    if (!await elasticsearchService.IndexExistsAsync())
                    {
                        _logger.LogWarning("Elasticsearch index doesn't exist, creating...");
                        await elasticsearchService.CreateIndexAsync();
                    }

                    // Optional: Periodic full reindex (be careful with this in production)
                    // await elasticsearchService.ReindexAllPropertiesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Elasticsearch sync job");
                }
            }
        }
    }
}