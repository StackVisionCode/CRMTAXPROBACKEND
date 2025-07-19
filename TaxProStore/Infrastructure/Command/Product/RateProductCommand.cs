using Application.Common;
using Application.Dtos;
using MediatR;

public record RateProductCommand(RateDto Data) : IRequest<ApiResponse<bool>>;
