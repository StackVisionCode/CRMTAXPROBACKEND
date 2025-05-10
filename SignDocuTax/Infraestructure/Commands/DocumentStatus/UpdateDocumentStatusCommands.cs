using Common;
using DTOs.DocumentsStatus;
using MediatR;

namespace Commands.DocumentsType;

public record  class UpdateDocumentStatusCommands(CreateNewDocumentsStatusDtos DocumentsStatus): IRequest<ApiResponse<bool>>;