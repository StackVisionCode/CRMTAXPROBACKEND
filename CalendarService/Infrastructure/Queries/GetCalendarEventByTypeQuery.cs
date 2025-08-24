using Application.DTO;
using MediatR;

namespace Infrastructure.Queries;

public sealed record GetCalendarEventByTypeQuery(string Type) : IRequest<List<CalendarEventDto>>;

