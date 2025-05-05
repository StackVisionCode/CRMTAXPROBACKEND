using AutoMapper;
using Commands.UserTypeCommands;
using UserDTOS;
using Users;

namespace AuthService.Profiles.User;

public class UserTypeProfile : Profile
{
    public UserTypeProfile()
    {
        CreateMap<TaxUserType, TaxUserTypeDTO>().ReverseMap();
        CreateMap<TaxUserType, CreateTaxUserTypeCommands>().ReverseMap();
        CreateMap<TaxUserType, UpdateTaxUserTypeCommands>().ReverseMap();
    }
}