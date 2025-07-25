using Application.Dtos.Form;
using AutoMapper;
using Domain.Entity.Form;

namespace Application.Profiles;

public class FormInstanceProfile : Profile
{
    public FormInstanceProfile()
    {
 
        CreateMap<FormInstance, FormInstanceDto>().ReverseMap();
        CreateMap<FormInstance, CreateFormInstanceDto>().ReverseMap();
        // Mapeo para edición si tienes un DTO distinto para actualizar
        // CreateMap<UpdateFormInstanceDto, FormInstance>()
        //     .ForMember(dest => dest.Id, opt => opt.Ignore())
        //     .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

         CreateMap<FormInstanceDto,FormInstance>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TemplateId, opt => opt.MapFrom(src => src.TemplateId))
            .ForMember(dest => dest.OwnerUserId, opt => opt.MapFrom(src => src.OwnerUserId))
            .ForMember(dest => dest.CustomTitle, opt => opt.MapFrom(src => src.CustomTitle))
            .ForMember(dest => dest.Template, opt => opt.MapFrom(src => src.Template))
            .ForMember(dest => dest.Responses, opt => opt.MapFrom(src => src.Responses));
    }
}
