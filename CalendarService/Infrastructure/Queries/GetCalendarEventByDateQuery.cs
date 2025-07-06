using Application.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record class GetCalendarEventByDateQuery(DateTime date) : IRequest<List<CalendarEventDto>>;
