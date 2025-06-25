using CommLinkServices.Application.DTOs;
using Common;
using MediatR;

namespace CommLinkServices.Infrastructure.Commands;

public record class EndCallCommand(Guid RequesterId, EndCallRequestDto Payload)
    : IRequest<ApiResponse<CallEndedDto>>;
