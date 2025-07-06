using AutoMapper;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;

namespace Applicaction.Handlers;
public class UpdateCalendarEventHandler : IRequestHandler<UpdateCalendarEventCommand, bool>
{
    private readonly CalendarDbContext _context;
    private readonly IMapper _mapper;

    public UpdateCalendarEventHandler(CalendarDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<bool> Handle(UpdateCalendarEventCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.CalendarEvents.FindAsync(request.EventDto.Id,cancellationToken);
        if (entity == null) return false;

        _mapper.Map(request.EventDto, entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
