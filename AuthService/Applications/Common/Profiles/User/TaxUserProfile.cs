using AutoMapper;
using UserDTOS;

namespace AuthService.Profiles.User;

public class TaxUserProfile : Profile
{
  public TaxUserProfile()
  {
    CreateMap<TaxUserProfileDTO, TaxUserProfile>().ReverseMap();
  }
}