using SharedLibrary.DTOs;

public interface IEmailConfigProvider
{
  SmtpConfig GetConfigForEvent(IntegrationEvent evt);
}
