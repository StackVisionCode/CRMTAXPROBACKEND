using Common;
using DTOs.DocumentsType;
using MediatR;

namespace Commands.DocumentsType;

public record  class CreateDocumentTypeCommands(CreateNewDocumentsTypeDTo DocumentsType): IRequest<ApiResponse<bool>>;