using Application.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Applicaction.Handlers;

public class GetCalendarEventByUserHandler : IRequestHandler<GetCalendarEventByUserQuery, List<CalendarEventDto>>
{
    private readonly CalendarDbContext _context;
    private readonly IMapper _mapper;

    public GetCalendarEventByUserHandler(CalendarDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CalendarEventDto>> Handle(GetCalendarEventByUserQuery request, CancellationToken cancellationToken)
    {
        var events = await _context.CalendarEvents
            .Where(e => e.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<CalendarEventDto>>(events);
    }
}
