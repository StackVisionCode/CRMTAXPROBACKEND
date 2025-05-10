using Common;
using DTOs.Signers;
using MediatR;

namespace Queries.Signers;

public record class GetDocumentSignersQuery(int DocumentId):IRequest<ApiResponse<List<ExternalSignerDto>>>;

