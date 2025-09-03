using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;

public record AddSpeakerCommand(SpeakerDTO Dto) : IRequest<Guid>;
