using Application.Common;
using Application.Dtos.Form;
using MediatR;

namespace Infrastructure.Command.Form;
public record CreateFormResponseCommand(FormResponseDto Response) : IRequest<ApiResponse<bool>>;
