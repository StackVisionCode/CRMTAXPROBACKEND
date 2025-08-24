
using Application.DTO;
using Domain.Entities;
using AutoMapper;
namespace Applications.DTO;
public class CalendarProfile : Profile
{
    public CalendarProfile()
    {
        // Base → entidades
        CreateMap<AppointmentDto, Appointment>();
        CreateMap<MeetingDto, Meeting>()
            .ForMember(d => d.Participants,
                opt => opt.MapFrom(src => src.Participants.Select(p => new EventParticipant
                {
                    Email = p.Email,
                    Name = p.Name
                })));

        // Entidad → DTO base (simplificado)
        CreateMap<CalendarEvents, CalendarEventDto>()
            .ForMember(d => d.StartUtc, o => o.MapFrom(s => s.StartUtc))
            .ForMember(d => d.EndUtc, o => o.MapFrom(s => s.EndUtc))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type));

        // Meeting → MeetingDto (para lecturas con participantes)
        CreateMap<Meeting, MeetingDto>()
            .IncludeBase<CalendarEvents, CalendarEventDto>()
            .ForMember(d => d.Participants,
                o => o.MapFrom(s => s.Participants.Select(p => new ParticipantDto { Email = p.Email, Name = p.Name })));
    }
}