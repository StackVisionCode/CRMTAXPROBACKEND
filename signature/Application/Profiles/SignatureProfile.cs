using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Profiles;


public class SignatureProfile : Profile
{
    public SignatureProfile()
    {
        CreateMap<Signature, CreateSignatureDto>().ReverseMap();
    
    }
}