using Application.Common.DTO;

public interface IEmailConfigProvider
{
    SmtpConfig GetConfigForEvent(EmailNotificationDto dto);
}
