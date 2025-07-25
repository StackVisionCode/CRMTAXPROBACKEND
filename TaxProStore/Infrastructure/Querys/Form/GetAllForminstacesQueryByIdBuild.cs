namespace Querys.Form;

using Application.Common;
using Application.Dtos.Form;
using MediatR;

public record GetAllForminstacesQueryByIdBuild(Guid Id) : IRequest<ApiResponse<List<FormInstanceDto>>>;
