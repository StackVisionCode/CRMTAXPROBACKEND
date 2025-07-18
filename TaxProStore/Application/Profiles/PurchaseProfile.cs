
using Application.Domain.Entity.Purchases;
using Application.Dtos;
using AutoMapper;

namespace Application.Profiles;

public class PurchaseProfile : Profile
{
    public PurchaseProfile()
    {
        CreateMap<PurchaseDto, Purchase>().ReverseMap();
    }
}