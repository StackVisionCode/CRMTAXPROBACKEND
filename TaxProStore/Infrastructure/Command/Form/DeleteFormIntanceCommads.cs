using Application.Common;
using MediatR;

namespace Infrastructure.Command.Form;

public record class DeleteFormIntanceCommads(Guid Id) : IRequest<ApiResponse<bool>>;