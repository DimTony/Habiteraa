using Habitera.DTOs;
using Habitera.Models;
using Habitera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Habitera.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly IPropertyService _propertyService;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyService propertyService,
            ILogger<PropertyController> logger)
        {
            _propertyService = propertyService;
            _logger = logger;
        }

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

        [HttpPut("{propertyId}")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UpdateProperty(Guid propertyId, [FromBody] UpdatePropertyDTO dto)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.UpdatePropertyAsync(agentId, propertyId, dto);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetProperty(Guid propertyId)
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


        //[HttpPost("{propertyId}/publish")]
        //[Authorize(Roles = "Agent")]
        //public async Task<IActionResult> PublishProperty(Guid propertyId)
        //{
        //    var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        //    var result = await _propertyService.PublishPropertyAsync(agentId, propertyId);

        //    return StatusCode(result.StatusCode, result);
        //}

        //[HttpGet("agent/dashboard")]
        //[Authorize(Roles = "Agent")]
        //public async Task<IActionResult> GetAgentDashboard()
        //{
        //    var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        //    var result = await _propertyService.GetAgentDashboardAsync(agentId);

        //    return StatusCode(result.StatusCode, result);
        //}

        // Controllers/PropertyController.cs - Add these endpoints

        [HttpPost("{propertyId}/images")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UploadPropertyImage(
            Guid propertyId,
            IFormFile image)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.AddPropertyImageAsync(agentId, propertyId, image);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{propertyId}/images/multiple")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> UploadMultiplePropertyImages(
            Guid propertyId,
            [FromForm] IFormFileCollection images)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.AddMultiplePropertyImagesAsync(agentId, propertyId, images);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{propertyId}/images/{imageId}")]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> DeletePropertyImage(Guid propertyId, Guid imageId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _propertyService.DeletePropertyImageAsync(agentId, propertyId, imageId);

            return StatusCode(result.StatusCode, result);
        }
    }
}