using AutoMapper;
using Habitera.Models;
using Habitera.DTOs;

namespace Habitera.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, UserDTO>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Profile.LastName))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Profile.City))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Profile.State))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Profile.Country))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src =>
                    src.Profile.Location != null && !double.IsNaN(src.Profile.Location.Y)
                        ? src.Profile.Location.Y
                        : (double?)null))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src =>
                    src.Profile.Location != null && !double.IsNaN(src.Profile.Location.X)
                        ? src.Profile.Location.X
                        : (double?)null))
                .ForMember(dest => dest.PreferredLanguage, opt => opt.MapFrom(src => src.Profile.PreferredLanguage))
                .ForMember(dest => dest.EmailNotifications, opt => opt.MapFrom(src => src.Profile.EmailNotifications))
                .ForMember(dest => dest.PushNotifications, opt => opt.MapFrom(src => src.Profile.PushNotifications))
                .ForMember(dest => dest.LicenseNumber, opt => opt.MapFrom(src => src.AgentProfile != null ? src.AgentProfile.LicenseNumber : null))
                .ForMember(dest => dest.AgencyName, opt => opt.MapFrom(src => src.AgentProfile != null ? src.AgentProfile.AgencyName : null))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.AgentProfile != null ? src.AgentProfile.AverageRating : (decimal?)null))
                .ForMember(dest => dest.TotalReviews, opt => opt.MapFrom(src => src.AgentProfile != null ? src.AgentProfile.TotalReviews : (int?)null));

           // CreateMap<UserProfile, UserProfileDTO>()
           //.ForMember(dest => dest.Latitude, opt => opt.MapFrom(src =>
           //    src.Location != null && !double.IsNaN(src.Location.Y) ? src.Location.Y : (double?)null))
           //.ForMember(dest => dest.Longitude, opt => opt.MapFrom(src =>
           //    src.Location != null && !double.IsNaN(src.Location.X) ? src.Location.X : (double?)null));

           // CreateMap<UpdateUserProfileDTO, UserProfile>()
           //     .ForMember(dest => dest.Location, opt => opt.MapFrom(src =>
           //         src.Latitude.HasValue && src.Longitude.HasValue
           //             ? new Point(src.Longitude.Value, src.Latitude.Value) { SRID = 4326 }
           //             : null))
           //     .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

           // CreateMap<AgentProfile, AgentProfileDTO>();

           // CreateMap<UpdateAgentProfileDTO, AgentProfile>()
           //     .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}