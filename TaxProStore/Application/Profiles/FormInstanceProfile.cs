using Application.Dtos.Form;
using AutoMapper;
using Domain.Entity.Form;

namespace Application.Profiles;

public class FormInstanceProfile : Profile
{
    public FormInstanceProfile()
    {
        // Mapeo general (incluye Id, fechas, etc.)
        CreateMap<FormInstanceDto, FormInstance>().ReverseMap();

        // Mapeo para creación (ignora Id, fechas automáticas)
        CreateMap<FormInstanceDto, FormInstance>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

    CreateMap<FormInstance, FormInstanceDto>().ReverseMap();
        CreateMap<FormInstance, CreateFormInstanceDto>().ReverseMap();
        // Mapeo para edición si tienes un DTO distinto para actualizar
        // CreateMap<UpdateFormInstanceDto, FormInstance>()
        //     .ForMember(dest => dest.Id, opt => opt.Ignore())
        //     .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}
