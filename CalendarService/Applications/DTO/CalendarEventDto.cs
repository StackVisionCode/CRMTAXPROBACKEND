namespace Application.DTO;
public class CalendarEventDto
{

     public Guid UserId { get; set; } // Foreign key to the user
     public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Type { get; set; } = string.Empty; // Appointment or Meeting
    public string CreatedBy { get; set; } = string.Empty;
    public TimeSpan ReminderBefore { get; set; } = TimeSpan.FromMinutes(60);
}