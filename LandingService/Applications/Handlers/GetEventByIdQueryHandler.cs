using LandingService.Applications.DTO;

using LandingService.Infrastructure.Context;
using LandingService.Infrastructure.queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandingService.Applications.Handlers;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, CreateEventDto>
{
    private readonly ApplicationDbContext _context;

    public GetEventByIdQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateEventDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Events
            .Include(e => e.Attendees)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (entity == null) throw new ("Event not found");

        return new CreateEventDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Subtitle = entity.Subtitle,
            Date = entity.Date,
            Description = entity.Description,
            Attendees = entity.Attendees.Select(a => new PersonDto
            {
                Name = a.Name,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber
            }).ToList(),
            Documents = entity.Documents.Select(d => new DocumentDto
            {
                FileName = d.FileName,
                FileUrl = d.FileUrl
            }).ToList()
        };
    }
}
