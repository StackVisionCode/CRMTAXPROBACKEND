// Application/Handlers/UpdateCalendarEventHandler.cs
using Application.DTO;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class UpdateCalendarEventHandler : IRequestHandler<UpdateCalendarEventCommand, bool>
{
    private readonly CalendarDbContext _db;
    private readonly IMapper _mapper;

    public UpdateCalendarEventHandler(CalendarDbContext db, IMapper mapper)
    { _db = db; _mapper = mapper; }

    public async Task<bool> Handle(UpdateCalendarEventCommand request, CancellationToken ct)
    {
        var payload = request.EventDto.Event;
        var entity = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == request.EventDto.Id, ct);
        if (entity is null) return false;

        // Campos comunes
        entity.Title = payload.Title;
        entity.Description = payload.Description;
        entity.StartUtc = payload.StartUtc;
        entity.EndUtc = payload.EndUtc;
        entity.CustomerId = payload.CustomerId;
        entity.ReminderBefore = payload.ReminderBefore;

        // Campos específicos según tipo
        switch (entity)
        {
            case Appointment ap when payload is AppointmentDto aDto:
                ap.Location = aDto.Location;
                ap.WithWhom = aDto.WithWhom;
                break;

            case Meeting mt when payload is MeetingDto mDto:
                mt.MeetingLink = mDto.MeetingLink;
                // Reemplazo simple de participantes (puedes optimizar diff)
                mt.Participants.Clear();
                foreach (var p in mDto.Participants)
                    mt.Participants.Add(new EventParticipant { Email = p.Email, Name = p.Name });
                break;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
