using Common;
using DTOs.Documents;
using MediatR;


namespace Queries.Documents;

public record class GetAllDocumentsQuery:IRequest<ApiResponse<List<ReadDocumentsDto>>>;