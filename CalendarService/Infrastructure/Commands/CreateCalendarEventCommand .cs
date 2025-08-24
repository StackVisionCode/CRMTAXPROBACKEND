using Application.DTO;
using MediatR;

namespace Infrastructure.Commands;

public sealed record CreateCalendarEventCommand(CalendarEventDto EventDto) : IRequest<Guid>;
