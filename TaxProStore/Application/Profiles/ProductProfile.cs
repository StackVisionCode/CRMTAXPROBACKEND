using Application.Domain.Entity.Products;
using Application.Dtos;
using AutoMapper;
namespace Application.Profiles;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<ProductDto, Product>().ReverseMap();
    }
}