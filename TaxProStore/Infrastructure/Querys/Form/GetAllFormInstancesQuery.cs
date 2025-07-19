namespace Querys.Form;

using Application.Common;
using Application.Dtos.Form;
using MediatR;

public record GetAllFormInstancesQuery : IRequest<ApiResponse<List<FormInstanceDto>>>;
