using Application.Common.DTO;
using AutoMapper;
using Domain;

namespace Application.Common.Profiles;

public class EmailProfile:Profile
{
    public EmailProfile()
    {
        CreateMap<EmailDTO,EmailMessage>().ReverseMap();
    }
}