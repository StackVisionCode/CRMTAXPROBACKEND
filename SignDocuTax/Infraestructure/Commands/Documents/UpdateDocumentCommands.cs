using Common;
using DTOs.Documents;
using MediatR;

namespace Commands.Documents;

public record  class UpdateDocumentCommands(UpdateDocumentDto Documents): IRequest<ApiResponse<bool>>;