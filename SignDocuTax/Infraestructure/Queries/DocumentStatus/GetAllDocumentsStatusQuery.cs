using Common;
using DTOs.DocumentsStatus;
using MediatR;


namespace Queries.DocumentStatus;

public record class GetAllDocumentsStatusQuery:IRequest<ApiResponse<List<ReadDocumentsDtosStatus>>>;