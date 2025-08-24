using Application.DTO;
using MediatR;

namespace Infrastructure.Queries;

public sealed record GetCalendarEventByDateQuery(DateTimeOffset Date) : IRequest<List<CalendarEventDto>>;