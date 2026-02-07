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
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IRedisCacheService cache,
            ILogger<HealthController> logger)
        {
            _cache = cache;
            _logger = logger;
        }
    
    }
}
