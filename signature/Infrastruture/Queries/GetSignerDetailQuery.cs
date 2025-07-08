using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public record class GetSignerDetailQuery(Guid SignerId) : IRequest<ApiResponse<SignerDetailDto>>;
