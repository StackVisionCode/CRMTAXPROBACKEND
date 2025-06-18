using Application.Helpers;
using MediatR;

namespace signature.Infrastruture.Queries;

public record ValidateTokenQuery(string Token) : IRequest<ApiResponse<bool>>;
