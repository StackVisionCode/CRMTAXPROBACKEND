using Domain;

namespace Infrastructure.Services;

public interface IEmailService
{
    Task SendEmailAsync(Email email, EmailConfig config);
}
