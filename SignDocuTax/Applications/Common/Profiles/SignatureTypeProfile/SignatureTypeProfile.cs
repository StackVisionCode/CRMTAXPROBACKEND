using AutoMapper;
using Domains.Signatures;
using Dtos.SignatureTypeDto;

namespace Mappers
{
    public class SignatureTypeProfile : Profile
    {
        public SignatureTypeProfile()
        {
            CreateMap<CreateSignatureTypeDto, SignatureType>();
            CreateMap<UpdateSignatureTypeDto, SignatureType>();
            CreateMap<SignatureType, SignatureTypeDto>();
        }
    }
}
