using Application.Domain.Entity.Products;
using Application.Dtos;
using AutoMapper;
namespace Application.Profiles;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<ProductDto, Product>().ReverseMap();
        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Likes, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(_ => 0.0))
            .ForMember(dest => dest.TotalRatings, opt => opt.MapFrom(_ => 0))
            .ReverseMap();
    }
}