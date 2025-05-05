using AutoMapper;
using Domain.Documents;
using DTOs.Documents;

namespace Application.Mappings
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            // Crear un nuevo documento
            CreateMap<CreateNewDocumentsDto, Document>();
            CreateMap<Document, CreateNewDocumentsDto>();

            // Actualizar un documento
            CreateMap<UpdateDocumentDto, Document>();
            CreateMap<Document, UpdateDocumentDto>();

            // Eliminar (aunque realmente para eliminar solo necesitas el Id, a veces se puede mapear)
            CreateMap<DeleteDocumentsDto, Document>();
            CreateMap<Document, DeleteDocumentsDto>();


            // Mapping Document to ReadDocumentsDto (should cover your List mapping)
            CreateMap<Document, ReadDocumentsDto>()
                .ForMember(dest => dest.DocumentStatusName, opt => opt.MapFrom(src =>  src.DocumentStatus.Name))
                .ForMember(dest => dest.DocumentTypeName, opt => opt.MapFrom(src => src.DocumentTypes.Name));

            // Leer documento por Id (opcional, si quieres diferenciar)
            CreateMap<ReadDocumentByIdDto, Document>();
            CreateMap<Document, ReadDocumentByIdDto>();
        }
    }
}
