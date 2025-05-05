using Common;
using DTOs.DocumentsType;
using MediatR;


namespace Queries.Documents;

public record class GetDocumentsTypeByIdQuery (ReadDocumentsTypeById DocumentsType) : IRequest<ApiResponse<ReadDocumentsType>>;
