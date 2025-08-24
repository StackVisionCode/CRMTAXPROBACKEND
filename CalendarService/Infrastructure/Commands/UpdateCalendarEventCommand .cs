using Application.DTO;
using Applications.DTO;
using MediatR;

namespace Infrastructure.Commands;
public sealed record UpdateCalendarEventCommand(CalendarEventUpdateDto EventDto) : IRequest<bool>;