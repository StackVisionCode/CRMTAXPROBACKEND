using Application.DTO;
using Applications.DTO;
using MediatR;

namespace Infrastructure.Commands;
public record class UpdateCalendarEventCommand (CalendarEventDtoGeneral EventDto) : IRequest<bool>;