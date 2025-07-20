using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public sealed record GetSignersQuery() : IRequest<ApiResponse<List<SignerListItemDto>>>;
