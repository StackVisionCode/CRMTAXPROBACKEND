using Application.DTO;
using MediatR;


namespace Infrastructure.Queries;
public sealed record GetCalendarEventByUserQuery(Guid UserId) : IRequest<List<CalendarEventDto>>;


