// Application/Handlers/GetCalendarEventByUserHandler.cs
using Application.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetCalendarEventByUserHandler : IRequestHandler<GetCalendarEventByUserQuery, List<CalendarEventDto>>
{
    private readonly CalendarDbContext _db;
    private readonly IMapper _mapper;

    public GetCalendarEventByUserHandler(CalendarDbContext db, IMapper mapper)
    { _db = db; _mapper = mapper; }

    public async Task<List<CalendarEventDto>> Handle(GetCalendarEventByUserQuery request, CancellationToken ct)
    {
        var events = await _db.CalendarEvents
            .AsNoTracking()
            .Where(e => e.UserId == request.UserId)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(ct);

        return _mapper.Map<List<CalendarEventDto>>(events);
    }
}
