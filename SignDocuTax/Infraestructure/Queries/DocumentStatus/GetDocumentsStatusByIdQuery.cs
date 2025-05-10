using Common;
using DTOs.DocumentsStatus;
using MediatR;


namespace Queries.DocumentStatus;

public record class GetDocumentsStatusByIdQuery (ReadDocumentsDtosStatus DocumentsStatus):IRequest<ApiResponse<List<ReadDocumentsDtosStatus>>>;