using Common;
using DTOs.Documents;
using MediatR;

namespace Commands.Documents;

public record  class CreateDocumentCommands(CreateNewDocumentsDto Documents): IRequest<ApiResponse<bool>>;