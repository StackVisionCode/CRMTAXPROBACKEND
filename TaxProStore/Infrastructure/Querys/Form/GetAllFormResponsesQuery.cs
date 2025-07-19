using Application.Common;
using Application.Dtos.Form;
using MediatR;

public record GetAllFormResponsesQuery : IRequest<ApiResponse<List<FormResponseDto>>>;
