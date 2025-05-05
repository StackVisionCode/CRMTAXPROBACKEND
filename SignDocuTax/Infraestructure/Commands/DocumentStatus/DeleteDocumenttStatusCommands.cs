using Common;
using DTOs.DocumentsStatus;
using MediatR;

namespace Commands.DocumentsType;

public record  class DeleteDocumenttStatusCommands(DeleteDocumentsStatusDTo DocumentsStatus): IRequest<ApiResponse<bool>>;