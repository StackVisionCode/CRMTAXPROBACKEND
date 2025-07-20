using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public sealed record GetSignatureBoxesQuery()
    : IRequest<ApiResponse<List<SignatureBoxListItemDto>>>;
