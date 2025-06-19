using Application.DTOS;
using AutoMapper;
using Domain.Entities;

namespace Application.Profiles;

public class StatementProfile : Profile
{ public StatementProfile()
        {
            CreateMap<BankStatement, StatementDto>()
                .ForMember(dest => dest.StatementDate, opt => opt.MapFrom(src => src.StatementDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate.ToString("yyyy-MM-dd")))
                .ForMember(dest => dest.UploadedAt, opt => opt.MapFrom(src => src.UploadedAt.ToString("yyyy-MM-dd HH:mm"))).ReverseMap();

            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToString("yyyy-MM-dd"))).ReverseMap();
        }
    }
