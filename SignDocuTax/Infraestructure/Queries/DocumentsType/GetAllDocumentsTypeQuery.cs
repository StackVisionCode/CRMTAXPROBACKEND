using Common;
using DTOs.DocumentsType;
using MediatR;


namespace Queries.Documents;

public record class GetAllDocumentsTypeQuery:IRequest<ApiResponse<List<ReadDocumentsType>>>;