using AutoMapper;
using Domain.Documents;
using DTOs.DocumentsStatus;

namespace Profiles.DocumentStatusDto;

public class DocumentsStatusProfile : Profile
{
    public DocumentsStatusProfile()
    {
        CreateMap<ReadDocumentsDtosStatus, DocumentStatus>().ReverseMap();
        CreateMap<CreateNewDocumentsStatusDtos, DocumentStatus>().ReverseMap();
       
    }
}