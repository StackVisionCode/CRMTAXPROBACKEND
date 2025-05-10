using AutoMapper;
using Domains.Contacts;
using DTOs.Contacts;


namespace Profiles.Contacts;

public class ContactProfile : Profile
{
    public ContactProfile()
    {
        CreateMap<Contact, ContactDto>().ReverseMap();
        CreateMap<CreateContactDto, Contact>().ReverseMap();
        
        //Update mapping

        CreateMap<UpdateContactDto, Contact>().ReverseMap();
        CreateMap<Contact, UpdateContactDto>().ReverseMap();
    }
}

