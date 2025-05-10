using Common;
using DTOs.DocumentsType;
using MediatR;

namespace Commands.DocumentsType;

public record  class DeleteDocumentTypeCommands(DeleteDocumentsTypeDTo DocumentsType): IRequest<ApiResponse<bool>>;