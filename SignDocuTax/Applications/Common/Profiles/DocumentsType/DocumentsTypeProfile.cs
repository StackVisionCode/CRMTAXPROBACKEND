using AutoMapper;
using DTOs.DocumentsType;
using Domain.Documents;
using Commands.DocumentsType;
using Queries.Documents;

namespace Profiles.DocumentsType;

public class DocumentsTypeProfile : Profile
{
    public DocumentsTypeProfile()
    {
        CreateMap<ReadDocumentsType, DocumentType>().ReverseMap();
        CreateMap<CreateNewDocumentsTypeDTo, DocumentType>().ReverseMap();
        CreateMap<CreateDocumentTypeCommands, DocumentType>();
        CreateMap<UpdateDocumentTypeCommands, DocumentType>();
        CreateMap<GetDocumentsByIdQuery, DocumentType>();
    }
}