using Application.Helpers;
using MediatR;
using signature.Application.DTOs;

namespace signature.Infrastruture.Queries;

public record class ValidateTokenQuery(string Token)
    : IRequest<ApiResponse<ValidateTokenResultDto>>;
