using BankStaments.Domain.Tbl_IA_Entity;

namespace Application.Interfaes;
public interface IDeepSeekService
{
    Task<DeepSeekApiResponse> ProcessBankDocumentAsync(Stream fileStream, string fileName, string accountNumber, Guid customerId);
    Task<DeepSeekApiResponse> GetProcessingStatusAsync(string requestId);
}
