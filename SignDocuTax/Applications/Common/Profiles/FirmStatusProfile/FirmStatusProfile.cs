using AutoMapper;
using Domains.Firms;
using Dtos.FirmsStatusDto;

namespace Mappers
{
    public class FirmStatusProfile : Profile
    {
        public FirmStatusProfile()
        {
            CreateMap<CreateFirmStatusDto, FirmStatus>();
            CreateMap<UpdateFirmStatusDto, FirmStatus>();
            CreateMap<FirmStatus, FirmStatusDto>();
        }
    }
}
