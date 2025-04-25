using AutoMapper;
using NavigationPlatform.Application.Features.Journeys.Queries.Models;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Journey mappings
            CreateMap<Journey, JourneyDto>()
                .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.FullName : "Unknown"));
        }
    }
} 