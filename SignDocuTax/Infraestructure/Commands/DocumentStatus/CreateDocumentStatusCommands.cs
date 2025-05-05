using Common;
using DTOs.DocumentsStatus;
using MediatR;

namespace Commands.DocumentsStatus;

public record  class CreateDocumentStatusCommands(CreateNewDocumentsStatusDtos DocumentsStatus): IRequest<ApiResponse<bool>>;