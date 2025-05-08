namespace CustomerService.Infrastructure.Services;

public interface IMessageConsumerService
{
    void StartConsuming();
    void StopConsuming();
}
