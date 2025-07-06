using Application.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record class GetCalendarEventByTypeQuery (string type) : IRequest<List<CalendarEventDto>>;

