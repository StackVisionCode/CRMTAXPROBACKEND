using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;

public record class CreateEventCommand (CreateEventDto EventDto ) : IRequest<Guid>;
