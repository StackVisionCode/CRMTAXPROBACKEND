using Application.Common;
using Application.Dtos.Form;
using MediatR;

namespace Infrastructure.Command.Form;

public record class UpdateFormResponseCommand(Guid Id, FormResponseDto Response) : IRequest<ApiResponse<bool>>;