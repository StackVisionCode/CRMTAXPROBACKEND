using System.Globalization;
using System.Text;
using Application.Interfaes;
using Domain.Entities;

namespace BankStaments.Infrastructure.Services;


public class FileParser : IFileParser
{
    public async Task<BankStatement> ParseFileAsync(Stream fileStream, string fileName)
    {

        // Determinar el tipo de archivo basado en la extensión
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        return fileExtension switch
        {
            ".csv" => await ParseCsvFileAsync(fileStream),
            ".ofx" => await ParseOfxFileAsync(fileStream),
            ".qfx" => await ParseQfxFileAsync(fileStream),
            ".txt" => await ParseTxtFileAsync(fileStream, fileName),
            _ => throw new NotSupportedException($"Formato de archivo no soportado: {fileExtension}")
        };
    }
        
         private async Task<BankStatement> ParseCsvFileAsync(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                throw new FormatException("El archivo CSV no tiene el formato correcto");

            // Parsear encabezado (primera línea)
            var headerParts = lines[0].Split(',');
            var statement = new BankStatement
            {
                AccountNumber = headerParts.Length > 0 ? headerParts[0] : "N/A",
                StatementDate = headerParts.Length > 1 ? DateTime.ParseExact(headerParts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow,
                StartDate = headerParts.Length > 2 ? DateTime.ParseExact(headerParts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow,
                EndDate = headerParts.Length > 3 ? DateTime.ParseExact(headerParts[3], "yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow,
                OpeningBalance = headerParts.Length > 4 ? decimal.Parse(headerParts[4]) : 0,
                ClosingBalance = headerParts.Length > 5 ? decimal.Parse(headerParts[5]) : 0
            };

            // Parsear transacciones (líneas siguientes)
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 4) continue;

                var amount = decimal.Parse(parts[2]);
                var transaction = new Transaction
                {
                    Date = DateTime.ParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Description = parts[1],
                    Amount = Math.Abs(amount),
                    TransactionType = amount >= 0 ? "Credit" : "Debit",
                    Category = parts.Length > 4 ? parts[4] : null!
                };

                statement.Transactions.Add(transaction);
            }

            return statement;
        }

        private async Task<BankStatement> ParseOfxFileAsync(Stream fileStream)
        {
            // Implementación básica para archivos OFX (Open Financial Exchange)
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var statement = new BankStatement
            {
                StatementDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                OpeningBalance = 0,
                ClosingBalance = 0
            };

            // Parsear información básica del OFX
            var acctIdMatch = System.Text.RegularExpressions.Regex.Match(content, "<ACCTID>(.*?)<");
            statement.AccountNumber = acctIdMatch.Success ? acctIdMatch.Groups[1].Value : "N/A";

            var dtStartMatch = System.Text.RegularExpressions.Regex.Match(content, "<DTSTART>(.*?)<");
            if (dtStartMatch.Success)
            {
                var dtStart = dtStartMatch.Groups[1].Value;
                statement.StartDate = ParseOfxDate(dtStart);
            }

            var dtEndMatch = System.Text.RegularExpressions.Regex.Match(content, "<DTEND>(.*?)<");
            if (dtEndMatch.Success)
            {
                var dtEnd = dtEndMatch.Groups[1].Value;
                statement.EndDate = ParseOfxDate(dtEnd);
            }

            // Parsear transacciones
            var transactionMatches = System.Text.RegularExpressions.Regex.Matches(content, "<STMTTRN>.*?<TRNTYPE>(.*?)<.*?<DTPOSTED>(.*?)<.*?<TRNAMT>(.*?)<.*?<MEMO>(.*?)<.*?<STMTTRN>", 
                System.Text.RegularExpressions.RegexOptions.Singleline);

            foreach (System.Text.RegularExpressions.Match match in transactionMatches)
            {
                var trnType = match.Groups[1].Value;
                var dtPosted = match.Groups[2].Value;
                var trnAmt = match.Groups[3].Value;
                var memo = match.Groups[4].Value;

                var amount = decimal.Parse(trnAmt, CultureInfo.InvariantCulture);
                var transaction = new Transaction
                {
                    Date = ParseOfxDate(dtPosted),
                    Description = memo,
                    Amount = Math.Abs(amount),
                    TransactionType = trnType.ToUpper() == "CREDIT" ? "Credit" : "Debit"
                };

                statement.Transactions.Add(transaction);
            }

            // Calcular balances si no están en el archivo
            if (statement.OpeningBalance == 0 && statement.ClosingBalance == 0 && statement.Transactions.Any())
            {
                statement.ClosingBalance = statement.Transactions
                    .Where(t => t.TransactionType == "Credit").Sum(t => t.Amount) -
                    statement.Transactions.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
                
                statement.OpeningBalance = statement.ClosingBalance - 
                    statement.Transactions.Where(t => t.TransactionType == "Credit").Sum(t => t.Amount) +
                    statement.Transactions.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
            }

            return statement;
        }

        private async Task<BankStatement> ParseQfxFileAsync(Stream fileStream)
        {
            // QFX es similar a OFX, así que reutilizamos el parser OFX
            return await ParseOfxFileAsync(fileStream);
        }

        private async Task<BankStatement> ParseTxtFileAsync(Stream fileStream, string fileName)
        {
            // Implementación para archivos de texto con formato fijo
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var lines = (await reader.ReadToEndAsync())
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var statement = new BankStatement
            {
                AccountNumber = Path.GetFileNameWithoutExtension(fileName),
                StatementDate = DateTime.UtcNow,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                OpeningBalance = 0,
                ClosingBalance = 0
            };

            foreach (var line in lines)
            {
                if (line.Length < 10) continue; // Línea demasiado corta

                try
                {
                    var dateStr = line.Substring(0, 10).Trim();
                    var description = line.Length > 10 ? line.Substring(10, 30).Trim() : "";
                    var amountStr = line.Length > 40 ? line.Substring(40).Trim() : "0";

                    if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                        decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out var amount))
                    {
                        var transaction = new Transaction
                        {
                            Date = date,
                            Description = description,
                            Amount = Math.Abs(amount),
                            TransactionType = amount >= 0 ? "Credit" : "Debit"
                        };

                        statement.Transactions.Add(transaction);
                    }
                }
                catch
                {
                    // Ignorar líneas con formato incorrecto
                    continue;
                }
            }

            // Calcular balances
            if (statement.Transactions.Any())
            {
                statement.ClosingBalance = statement.Transactions
                    .Where(t => t.TransactionType == "Credit").Sum(t => t.Amount) -
                    statement.Transactions.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
                
                statement.OpeningBalance = statement.ClosingBalance - 
                    statement.Transactions.Where(t => t.TransactionType == "Credit").Sum(t => t.Amount) +
                    statement.Transactions.Where(t => t.TransactionType == "Debit").Sum(t => t.Amount);
            }

            return statement;
        }

        private DateTime ParseOfxDate(string ofxDate)
        {
            // Formato OFX: YYYYMMDDHHMMSS o YYYYMMDD
            if (ofxDate.Length >= 8 && 
                DateTime.TryParseExact(
                    ofxDate.Substring(0, 8), 
                    "yyyyMMdd", 
                    CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, 
                    out var date))
            {
                return date;
            }
            return DateTime.UtcNow;
        }
}