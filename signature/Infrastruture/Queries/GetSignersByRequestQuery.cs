using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public record class GetSignersByRequestQuery(Guid RequestId)
    : IRequest<ApiResponse<List<SignerDetailDto>>>;
