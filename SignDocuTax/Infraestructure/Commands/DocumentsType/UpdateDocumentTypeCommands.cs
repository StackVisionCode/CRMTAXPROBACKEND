using Common;
using DTOs.DocumentsType;
using MediatR;

namespace Commands.DocumentsType;

public record  class UpdateDocumentTypeCommands(UpdateDocumentsTypeDTo DocumentsType): IRequest<ApiResponse<bool>>;