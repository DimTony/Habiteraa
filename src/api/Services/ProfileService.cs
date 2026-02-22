using AutoMapper;
using Habitera.DTOs;
using Habitera.Models;
using Habitera.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Habitera.Services
{
    public interface IProfileService
    {
        Task<OperationResult<ProfileCompletionStatusDTO>> GetProfileCompletionStatusAsync(Guid userId);
        Task<OperationResult<AuthResponseDTO>> CompleteRegularUserProfileAsync(Guid userId, CompleteRegularUserProfileDTO dto);
        Task<OperationResult<AuthResponseDTO>> CompleteAgentProfileAsync(Guid userId, CompleteAgentProfileDTO dto);
        Task<OperationResult<ApplicationUserDTO>> UpdateRegularUserProfileAsync(Guid userId, CompleteRegularUserProfileDTO dto);
        Task<OperationResult<ApplicationUserDTO>> UpdateAgentProfileAsync(Guid userId, CompleteAgentProfileDTO dto);
        Task<OperationResult<ApplicationUserDTO>> GetUserProfileAsync(Guid userId);
    }

    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ITokenService tokenService,
            ILogger<ProfileService> logger)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<OperationResult<ProfileCompletionStatusDTO>> GetProfileCompletionStatusAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .Include(u => u.AgentProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<ProfileCompletionStatusDTO>.Failure("User not found", 404);

                var status = new ProfileCompletionStatusDTO
                {
                    IsProfileCompleted = user.ProfileCompleted,
                    UserType = user.UserType,
                    MissingFields = new List<string>()
                };

                if (user.UserType == UserType.User || user.UserType == UserType.Admin)
                {
                    if (string.IsNullOrWhiteSpace(user.Profile?.FirstName))
                        status.MissingFields.Add("FirstName");
                    if (string.IsNullOrWhiteSpace(user.Profile?.LastName))
                        status.MissingFields.Add("LastName");
                    if (string.IsNullOrWhiteSpace(user.Profile?.City))
                        status.MissingFields.Add("City");
                    if (string.IsNullOrWhiteSpace(user.Profile?.State))
                        status.MissingFields.Add("State");
                    if (string.IsNullOrWhiteSpace(user.Profile?.Country))
                        status.MissingFields.Add("Country");
                    if (user.Profile?.Location == null || (user.Profile.Location.X == 0 && user.Profile.Location.Y == 0))
                        status.MissingFields.Add("Location");
                }
                else if (user.UserType == UserType.Agent)
                {
                    if (string.IsNullOrWhiteSpace(user.Profile?.FirstName))
                        status.MissingFields.Add("FirstName");
                    if (string.IsNullOrWhiteSpace(user.Profile?.LastName))
                        status.MissingFields.Add("LastName");
                }

                var totalFields = user.UserType == UserType.Agent ? 2 : 6;
                var completedFields = totalFields - status.MissingFields.Count;
                status.CompletionPercentage = (int)((completedFields / (double)totalFields) * 100);

                return OperationResult<ProfileCompletionStatusDTO>.Successful(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile completion status for user {UserId}", userId);
                return OperationResult<ProfileCompletionStatusDTO>.Failure(
                    "An error occurred while checking profile status", 500);
            }
        }

        public async Task<OperationResult<AuthResponseDTO>> CompleteRegularUserProfileAsync(
            Guid userId,
            CompleteRegularUserProfileDTO dto)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<AuthResponseDTO>.Failure("User not found", 404);

                if (user.UserType != UserType.User && user.UserType != UserType.Admin)
                    return OperationResult<AuthResponseDTO>.Failure("Invalid user type", 400);

                if (user.ProfileCompleted)
                    return OperationResult<AuthResponseDTO>.Failure("Profile already completed", 400);

                // Update profile
                user.Profile.FirstName = dto.FirstName.Trim();
                user.Profile.LastName = dto.LastName.Trim();
                user.Profile.City = dto.City.Trim();
                user.Profile.State = dto.State.Trim();
                user.Profile.Country = dto.Country.Trim();
                user.Profile.Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };
                user.Profile.PreferredLanguage = dto.PreferredLanguage;

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber.Trim();
                }

                user.ProfileCompleted = true;
                user.ProfileCompletedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Generate new tokens with updated profile
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.GenerateAccessToken(user, roles);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync(
                    user.Id, dto.DeviceInfo, dto.IpAddress);

                var userDto = _mapper.Map<RegularUserDTO>(user);

                var authResponse = new AuthResponseDTO
                {
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    User = userDto
                };

                _logger.LogInformation("Profile completed for user {UserId}", userId);

                return OperationResult<AuthResponseDTO>.Successful(
                    authResponse,
                    "Profile completed successfully",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing profile for user {UserId}", userId);
                return OperationResult<AuthResponseDTO>.Failure(
                    "An error occurred while completing profile", 500);
            }
        }

        public async Task<OperationResult<AuthResponseDTO>> CompleteAgentProfileAsync(
            Guid userId,
            CompleteAgentProfileDTO dto)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .Include(u => u.AgentProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<AuthResponseDTO>.Failure("User not found", 404);

                if (user.UserType != UserType.Agent)
                    return OperationResult<AuthResponseDTO>.Failure("Invalid user type", 400);

                if (user.ProfileCompleted)
                    return OperationResult<AuthResponseDTO>.Failure("Profile already completed", 400);

                // Update basic profile
                user.Profile.FirstName = dto.FirstName.Trim();
                user.Profile.LastName = dto.LastName.Trim();
                user.Profile.City = dto.City.Trim();
                user.Profile.State = dto.State.Trim();
                user.Profile.Country = dto.Country.Trim();
                user.Profile.Location = new Point(0, 0) { SRID = 4326 }; // Agents don't need precise location

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber.Trim();
                }

                // Update agent-specific profile
                if (user.AgentProfile != null)
                {
                    user.AgentProfile.LicenseNumber = dto.LicenseNumber?.Trim();
                    user.AgentProfile.AgencyName = dto.AgencyName?.Trim();
                }

                user.ProfileCompleted = true;
                user.ProfileCompletedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Generate new tokens
                var roles = await _userManager.GetRolesAsync(user);
                var accessToken = _tokenService.GenerateAccessToken(user, roles);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync(
                    user.Id, dto.DeviceInfo, dto.IpAddress);

                var userDto = _mapper.Map<AgentUserDTO>(user);

                var authResponse = new AuthResponseDTO
                {
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    User = userDto
                };

                _logger.LogInformation("Agent profile completed for user {UserId}", userId);

                return OperationResult<AuthResponseDTO>.Successful(
                    authResponse,
                    "Profile completed successfully",
                    200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing agent profile for user {UserId}", userId);
                return OperationResult<AuthResponseDTO>.Failure(
                    "An error occurred while completing profile", 500);
            }
        }

        public async Task<OperationResult<ApplicationUserDTO>> UpdateRegularUserProfileAsync(
            Guid userId,
            CompleteRegularUserProfileDTO dto)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<ApplicationUserDTO>.Failure("User not found", 404);

                // Update profile
                user.Profile.FirstName = dto.FirstName.Trim();
                user.Profile.LastName = dto.LastName.Trim();
                user.Profile.City = dto.City.Trim();
                user.Profile.State = dto.State.Trim();
                user.Profile.Country = dto.Country.Trim();
                user.Profile.Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };
                user.Profile.PreferredLanguage = dto.PreferredLanguage;

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber.Trim();
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userDto = _mapper.Map<RegularUserDTO>(user);

                return OperationResult<ApplicationUserDTO>.Successful(
                    userDto,
                    "Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return OperationResult<ApplicationUserDTO>.Failure(
                    "An error occurred while updating profile", 500);
            }
        }

        public async Task<OperationResult<ApplicationUserDTO>> UpdateAgentProfileAsync(
            Guid userId,
            CompleteAgentProfileDTO dto)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .Include(u => u.AgentProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<ApplicationUserDTO>.Failure("User not found", 404);

                // Update profiles
                user.Profile.FirstName = dto.FirstName.Trim();
                user.Profile.LastName = dto.LastName.Trim();
                user.Profile.City = dto.City.Trim();
                user.Profile.State = dto.State.Trim();
                user.Profile.Country = dto.Country.Trim();

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber.Trim();
                }

                if (user.AgentProfile != null)
                {
                    user.AgentProfile.LicenseNumber = dto.LicenseNumber?.Trim();
                    user.AgentProfile.AgencyName = dto.AgencyName?.Trim();
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userDto = _mapper.Map<AgentUserDTO>(user);

                return OperationResult<ApplicationUserDTO>.Successful(
                    userDto,
                    "Profile updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent profile for user {UserId}", userId);
                return OperationResult<ApplicationUserDTO>.Failure(
                    "An error occurred while updating profile", 500);
            }
        }

        public async Task<OperationResult<ApplicationUserDTO>> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.Profile)
                    .Include(u => u.AgentProfile)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return OperationResult<ApplicationUserDTO>.Failure("User not found", 404);

                ApplicationUserDTO userDto = user.UserType == UserType.Agent
                    ? _mapper.Map<AgentUserDTO>(user)
                    : _mapper.Map<RegularUserDTO>(user);

                return OperationResult<ApplicationUserDTO>.Successful(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                return OperationResult<ApplicationUserDTO>.Failure(
                    "An error occurred while retrieving profile", 500);
            }
        }
    }
}