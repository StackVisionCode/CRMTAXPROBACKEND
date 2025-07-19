using Application.Common;
using Application.Dtos.Form;
using MediatR;

namespace Infrastructure.Command.Form;

public record class CreateFormIntanceCommads(CreateFormInstanceDto FormInstanceDto) : IRequest<ApiResponse<bool>>;