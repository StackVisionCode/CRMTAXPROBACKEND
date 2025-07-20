using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public record GetSigningLayoutQuery(string Token) : IRequest<ApiResponse<SigningLayoutDto>>;
