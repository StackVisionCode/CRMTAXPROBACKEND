namespace Services.Contracts;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body);
}