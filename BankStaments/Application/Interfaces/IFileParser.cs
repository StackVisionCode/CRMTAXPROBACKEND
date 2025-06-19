using Domain.Entities;

namespace Application.Interfaes;
   public interface IFileParser
    {
        Task<BankStatement> ParseFileAsync(Stream fileStream, string fileName);
    }