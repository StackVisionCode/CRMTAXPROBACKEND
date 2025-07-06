using Application.DTO;
using MediatR;


namespace Infrastructure.Queries;
public record class GetCalendarEventByUserQuery(Guid UserId) : IRequest<List<CalendarEventDto>>;


