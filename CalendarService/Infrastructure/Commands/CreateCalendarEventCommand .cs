using Application.DTO;
using MediatR;

namespace Infrastructure.Commands;

public record class CreateCalendarEventCommand (CalendarEventDto EventDto) : IRequest<Guid>;
