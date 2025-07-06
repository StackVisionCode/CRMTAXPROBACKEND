using Application.DTO;
using AutoMapper;
using Domain.Entities;

namespace Applications.DTO;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // CreateMap<CalendarEvents, CalendarEventDto>().ReverseMap();
        // CreateMap<Appointment, AppointmentDto>().ReverseMap();
        // CreateMap<Meeting, MeetingDto>().ReverseMap();
          CreateMap<CalendarEvents, CalendarEventDto>().ReverseMap();
    }
}