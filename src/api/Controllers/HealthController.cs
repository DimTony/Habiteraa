using Habitera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Habitera.Controllers
{
    


    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IRedisCacheService _cache;
        private readonly IElasticsearchService _elasticsearchService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IRedisCacheService cache,
            IElasticsearchService elasticsearchService,
            ILogger<HealthController> logger)
        {
            _cache = cache;
            _elasticsearchService = elasticsearchService;
            _logger = logger;
        }

        [HttpGet("Search")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckSearchHealth()
        {
            var elasticHealthy = await _elasticsearchService.IndexExistsAsync();
            var redisHealthy = await _cache.ExistsAsync("health-check-key");

            return Ok(new
            {
                elasticsearch = elasticHealthy ? "Connected" : "Disconnected",
                redis = redisHealthy || !redisHealthy ? "Connected" : "Disconnected",
                timestamp = DateTime.UtcNow
            });
        }
    
    }
}
