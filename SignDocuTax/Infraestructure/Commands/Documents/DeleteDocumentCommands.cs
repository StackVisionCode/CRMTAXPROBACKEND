using Common;
using DTOs.Documents;
using MediatR;

namespace Commands.Documents;

public record  class DeleteDocumentCommands(DeleteDocumentsDto Documents): IRequest<ApiResponse<bool>>;