using LandingService.Applications.DTO;
using MediatR;


namespace LandingService.Infrastructure.queries;

public record class GetEventByIdQuery(Guid Id ) : IRequest<CreateEventDto>;
