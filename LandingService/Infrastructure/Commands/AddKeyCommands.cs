using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;
public record AddEventKeyCommand(EventKeyDTO Dto) : IRequest<Guid>;
