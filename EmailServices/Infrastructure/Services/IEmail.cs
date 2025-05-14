using Application.Common.DTO;


namespace Infrastructure.Services;

public interface IEmail
{
    Task<bool> SendEmail(EmailDTO emailDTO);
}