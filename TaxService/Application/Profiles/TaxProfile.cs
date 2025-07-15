using Application.DTOS;
using AutoMapper;
using Domain.Entities;

namespace Application.Profiles;

public class TaxProfile : Profile
{
    public TaxProfile()
    {
        CreateMap<TaxDto, Tax>()
            .ReverseMap();
    }
    // This class can be used to define AutoMapper profiles for mapping between domain models and DTOs related to taxes.
    // Currently, it is empty but can be extended in the future as needed.
}