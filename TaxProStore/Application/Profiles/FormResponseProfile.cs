

using Application.Dtos.Form;
using AutoMapper;
using Domain.Entity.Form;

namespace Application.Profiles;

public class FormResponseProfile : Profile
{
    public FormResponseProfile()
    {
        CreateMap<FormResponse, FormResponseDto>().ReverseMap();
        CreateMap<FormResponseDto, FormResponse>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // si Id se genera automáticamente
            .ForMember(dest => dest.FormInstance, opt => opt.Ignore()); // si quieres asignar manualmente
    }
}
