// Applicat*ion*/Handlers/CreateCalendarEventHandler.cs
using Application.DTO;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.Reminders;

namespace Application.Handlers;

public class CreateCalendarEventHandler : IRequestHandler<CreateCalendarEventCommand, Guid>
{
    private readonly CalendarDbContext _db;
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus;

    public CreateCalendarEventHandler(CalendarDbContext db, IMapper mapper, IEventBus eventBus)
    {
        _db = db;
        _mapper = mapper;
        _eventBus = eventBus;
    }

    public async Task<Guid> Handle(CreateCalendarEventCommand request, CancellationToken ct)
    {
        // Mapea según el tipo
        CalendarEvents entity = request.EventDto.Type.ToLowerInvariant() switch
        {
            "meeting" => _mapper.Map<Meeting>(request.EventDto as MeetingDto ?? new MeetingDto
            {
                // fallback si vino como CalendarEventDto
                Title = request.EventDto.Title,
                Description = request.EventDto.Description,
                StartUtc = request.EventDto.StartUtc,
                EndUtc = request.EventDto.EndUtc,
                UserId = request.EventDto.UserId,
                CustomerId = request.EventDto.CustomerId,
                CreatedBy = request.EventDto.CreatedBy,
                ReminderBefore = request.EventDto.ReminderBefore
            }),
            _ => _mapper.Map<Appointment>(request.EventDto as AppointmentDto ?? new AppointmentDto
            {
                Title = request.EventDto.Title,
                Description = request.EventDto.Description,
                StartUtc = request.EventDto.StartUtc,
                EndUtc = request.EventDto.EndUtc,
                UserId = request.EventDto.UserId,
                CustomerId = request.EventDto.CustomerId,
                CreatedBy = request.EventDto.CreatedBy,
                ReminderBefore = request.EventDto.ReminderBefore
            })
        };

        _db.CalendarEvents.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Publica el comando de integración para que ReminderService agende los recordatorios
        var defaultDays = Math.Max(0, (int)Math.Round(request.EventDto.ReminderBefore.TotalDays));

        var evt = new ScheduleEventRemindersRequested(
            Id: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            EventId: entity.Id,
            EventStartUtc: request.EventDto.StartUtc,
            DaysBefore: new[] { defaultDays },
            RemindAtTime: null, // usa el default (09:00) del ReminderService si aplica
            Message: $"Recordatorio: {entity.Title}",
            Channel: "email",
            UserId: request.EventDto.UserId.ToString()
        );

        _eventBus.Publish(evt);

        return entity.Id;
    }
}
