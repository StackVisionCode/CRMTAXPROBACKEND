namespace Services.Contracts;

public interface INotificationService
{
    Task NotifySignersAsync(int requirementId, string? customMessage = null);
}