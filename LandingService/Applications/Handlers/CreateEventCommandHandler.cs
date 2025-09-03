using LandingService.Domain;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.Context;
using MediatR;

namespace LandingService.Applications.Handlers;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly ApplicationDbContext _context;

    public CreateEventCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var entity = new Event
        {
            Title = request.EventDto.Title,
            Subtitle = request.EventDto.Subtitle,
            Date = request.EventDto.Date,
            Description = request.EventDto.Description,
            Attendees = request.EventDto.Attendees.Select(a => new Person
            {
                Name = a.Name,
                Email = a.Email,
                PhoneNumber = a.PhoneNumber
            }).ToList(),
            Documents = request.EventDto.Documents.Select(d => new Document
            {
                FileName = d.FileName,
                FileUrl = d.FileUrl
            }).ToList()
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
