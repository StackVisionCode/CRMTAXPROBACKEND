using AutoMapper;
using Domain.Entities;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;

namespace Applicaction.Handlers;

public class CreateCalendarEventHandler : IRequestHandler<CreateCalendarEventCommand, Guid>
{
    private readonly CalendarDbContext _context;
    private readonly IMapper _mapper;

    public CreateCalendarEventHandler(CalendarDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateCalendarEventCommand request, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<CalendarEvents>(request.EventDto);
        entity.Id = Guid.NewGuid();

        _context.CalendarEvents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}