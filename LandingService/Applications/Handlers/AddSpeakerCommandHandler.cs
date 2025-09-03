using LandingService.Domain;
using LandingService.Infrastructure.Commands;
using LandingService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LandingService.Applications.Handlers;

public class AddSpeakerCommandHandler : IRequestHandler<AddSpeakerCommand, Guid>
{
    private readonly ApplicationDbContext _context;

    public AddSpeakerCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddSpeakerCommand request, CancellationToken cancellationToken)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.Id == request.Dto.Id, cancellationToken);
        if (!eventExists)
            throw new KeyNotFoundException("Event not found");

        var speaker = new Speaker
        {
            Name = request.Dto.Name,
            EventId = request.Dto.EventId         
          
         
        };

        _context.Speakers.Add(speaker);
        await _context.SaveChangesAsync(cancellationToken);

        return speaker.Id;
    }
}

