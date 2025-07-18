using Application.Domain.Entity.Templates;
using Application.Dtos;
using AutoMapper;

namespace Application.Profiles;


public class TemplateProfile : Profile
{
    public TemplateProfile()
    {
        CreateMap<TemplateDto, Template>().ReverseMap();
        CreateMap<CreateDto, Template>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ReverseMap();
        CreateMap<UpdateTempladeDto, Template>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ReverseMap();
            
    }
}