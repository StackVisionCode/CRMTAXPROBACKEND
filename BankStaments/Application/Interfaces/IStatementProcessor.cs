using BankStaments.Domain.Tbl_IA_Entity;
using Domain.Entities;

namespace Application.Interfaes;
public interface IStatementProcessor
{
    Task<BankStatement> ProcessDeepSeekResponseAsync(
        DeepSeekApiResponse apiResponse,
        string accountNumber,
        string customerId,
        string documentType);
}