namespace Services.Contracts;

public interface IInternalMessagingService
{
    Task SendAsync(int userId, string title, string message);
}