using Common;
using DTOs.Documents;
using MediatR;


namespace Queries.Documents;

public record class GetDocumentsByIdQuery(ReadDocumentByIdDto documents):IRequest<ApiResponse<ReadDocumentsDto>>;