using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Application.Interfaes;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Domain.Entities;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

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
            ".xml" => await ParseXmlFileAsync(fileStream),
            ".json" => await ParseJsonFileAsync(fileStream),
            ".pdf" => await ParsePdfFileAsync(fileStream),
            ".txt" => await ParseTxtFileAsync(fileStream, fileName),
            ".xlsx" => await ParseXlsxFileAsync(fileStream),
            _ => throw new NotSupportedException(
                $"Formato de archivo no soportado: {fileExtension}"
            ),
        };
    }

    private static readonly Regex RangoFechasRegex = new(
        @"(?<ini>\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}).*?(Hasta|to).*?(?<fin>\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4})",
        RegexOptions.IgnoreCase
    );

    private static readonly Regex GeneradoRegex = new(
        @"Generado\s+el\s+(?<fecha>\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4})(\s+(?<hora>\d{1,2}:\d{2}(:\d{2})?))?",
        RegexOptions.IgnoreCase
    );
    private static readonly string[] CsvDateFormats = new[]
    {
        "dd/MM/yyyy",
        "d/M/yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "MM-dd-yyyy",
        "M-d-yyyy",
        "dd/MM/yy",
        "d/M/yy",
        "MM/dd/yy",
        "M/d/yy",
        "dd-MM-yy",
        "d-M-yy",
        "MM-dd-yy",
        "M-d-yy",
    };

    /// <summary>
    /// Intenta convertir cualquier string con los formatos admitidos.
    /// Devuelve false si no coincide ninguno.
    /// </summary>
    private static bool TryParseDateEnhanced(string value, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().Trim('"');

        // Probar todos los formatos explícitos
        if (
            DateTime.TryParseExact(
                value,
                CsvDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date
            )
        )
        {
            // Ajustar años de 2 dígitos (opcional)
            if (date.Year < 1950)
                date = date.AddYears(2000);
            else if (date.Year < 2000 && date.Year >= 50)
                date = date.AddYears(1900);

            return true;
        }

        // Fallback: intentar Parse estándar (por si trae mes en texto, etc.)
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(value, out date);
    }

    /// <summary>
    /// Parsea un archivo CSV y devuelve un objeto BankStatement
    /// </summary>
    /// <param name="fileStream"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    private async Task<BankStatement> ParseCsvFileAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 4)
            throw new FormatException("El archivo CSV no tiene el formato esperado");

        // ───── 1. Datos básicos ────────────────────────────────────────────
        var statement = new BankStatement
        {
            AccountNumber = lines[0].Trim(), // Línea 0: nombre o número de cuenta
            StatementDate = DateTime.UtcNow, // se ajusta abajo si encontramos “Generado el …”
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow,
        };

        // ───── 2. Buscar rango de fechas y fecha de generación ────────────
        foreach (var l in lines.Take(5)) // sólo las primeras líneas “de cabecera”
        {
            var rango = RangoFechasRegex.Match(l);
            if (
                rango.Success
                && TryParseDateEnhanced(rango.Groups["ini"].Value, out var ini)
                && TryParseDateEnhanced(rango.Groups["fin"].Value, out var fin)
            )
            {
                statement.StartDate = ini;
                statement.EndDate = fin;
            }

            var gen = GeneradoRegex.Match(l);
            if (gen.Success && TryParseDateEnhanced(gen.Groups["fecha"].Value, out var fechaGen))
            {
                if (TimeSpan.TryParse(gen.Groups["hora"].Value, out var hora))
                    fechaGen = fechaGen.Date + hora;

                statement.StatementDate = fechaGen;
            }
        }

        // ───── 3. Parsear transacciones (tu lógica ya funcionaba) ──────────
        for (int i = 4; i < lines.Length; i++) // empezamos después de las 4 primeras
        {
            var parts = lines[i].Split(',');

            if (parts.Length < 3)
                continue;

            if (!TryParseDateEnhanced(parts[0], out var txDate))
                continue;

            if (
                !decimal.TryParse(
                    parts[2].Trim('"').Replace(",", ""),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var amount
                )
            )
                continue;

            statement.Transactions.Add(
                new Transaction
                {
                    Date = txDate,
                    Description = parts.Length > 1 ? parts[1].Trim('"') : "Sin descripción",
                    Amount = Math.Abs(amount),
                    TransactionType = amount >= 0 ? "Credit" : "Debit",
                    Category = parts.Length > 4 ? parts[4].Trim('"') : null,
                }
            );
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
            ClosingBalance = 0,
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
        var transactionMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            "<STMTTRN>.*?<TRNTYPE>(.*?)<.*?<DTPOSTED>(.*?)<.*?<TRNAMT>(.*?)<.*?<MEMO>(.*?)<.*?<STMTTRN>",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

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
                TransactionType = trnType.ToUpper() == "CREDIT" ? "Credit" : "Debit",
            };

            statement.Transactions.Add(transaction);
        }

        // Calcular balances si no están en el archivo
        if (
            statement.OpeningBalance == 0
            && statement.ClosingBalance == 0
            && statement.Transactions.Any()
        )
        {
            statement.ClosingBalance =
                statement.Transactions.Where(t => t.TransactionType == "Credit").Sum(t => t.Amount)
                - statement
                    .Transactions.Where(t => t.TransactionType == "Debit")
                    .Sum(t => t.Amount);

            statement.OpeningBalance =
                statement.ClosingBalance
                - statement
                    .Transactions.Where(t => t.TransactionType == "Credit")
                    .Sum(t => t.Amount)
                + statement
                    .Transactions.Where(t => t.TransactionType == "Debit")
                    .Sum(t => t.Amount);
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
        var lines = (await reader.ReadToEndAsync()).Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        var statement = new BankStatement
        {
            AccountNumber = Path.GetFileNameWithoutExtension(fileName),
            StatementDate = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            OpeningBalance = 0,
            ClosingBalance = 0,
        };

        foreach (var line in lines)
        {
            if (line.Length < 10)
                continue; // Línea demasiado corta

            try
            {
                var dateStr = line.Substring(0, 10).Trim();
                var description = line.Length > 10 ? line.Substring(10, 30).Trim() : "";
                var amountStr = line.Length > 40 ? line.Substring(40).Trim() : "0";

                if (
                    DateTime.TryParseExact(
                        dateStr,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var date
                    )
                    && decimal.TryParse(
                        amountStr,
                        NumberStyles.Currency,
                        CultureInfo.InvariantCulture,
                        out var amount
                    )
                )
                {
                    var transaction = new Transaction
                    {
                        Date = date,
                        Description = description,
                        Amount = Math.Abs(amount),
                        TransactionType = amount >= 0 ? "Credit" : "Debit",
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
            statement.ClosingBalance =
                statement.Transactions.Where(t => t.TransactionType == "Credit").Sum(t => t.Amount)
                - statement
                    .Transactions.Where(t => t.TransactionType == "Debit")
                    .Sum(t => t.Amount);

            statement.OpeningBalance =
                statement.ClosingBalance
                - statement
                    .Transactions.Where(t => t.TransactionType == "Credit")
                    .Sum(t => t.Amount)
                + statement
                    .Transactions.Where(t => t.TransactionType == "Debit")
                    .Sum(t => t.Amount);
        }

        return statement;
    }

    /// <summary>
    /// Parser para archivos PDF
    /// </summary>
    /// <param name="fileStream"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    private async Task<BankStatement> ParsePdfFileAsync(Stream fileStream)
    {
        var statement = new BankStatement
        {
            AccountNumber = "PDF_ACCOUNT",
            StatementDate = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            OpeningBalance = 0,
            ClosingBalance = 0,
        };

        try
        {
            // Crear el lector de PDF
            using var pdfReader = new PdfReader(fileStream);
            using var pdfDocument = new PdfDocument(pdfReader);

            var allText = new StringBuilder();

            // Extraer texto de todas las páginas
            for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
            {
                var page = pdfDocument.GetPage(pageNum);
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                allText.AppendLine(pageText);
            }

            var fullText = allText.ToString();

            // Parsear información del statement usando expresiones regulares
            ParseStatementInfo(statement, fullText);

            // Parsear transacciones
            ParseTransactions(statement, fullText);

            return statement;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Error parsing PDF file: {ex.Message}", ex);
        }
    }

    private void ParseStatementInfo(BankStatement statement, string text)
    {
        // Patrones comunes para información del statement
        var patterns = new Dictionary<string, string>
        {
            // Número de cuenta - patrones diversos
            ["AccountNumber"] =
                @"(?:Account\s*(?:Number|No\.?)|Cuenta\s*(?:Número|No\.?)|A/C\s*No\.?)[:\s]*([A-Z0-9\-]{8,20})",

            // Fechas del statement
            ["StatementDate"] =
                @"(?:Statement\s*Date|Fecha\s*del?\s*Estado)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2})",
            ["StartDate"] =
                @"(?:From|Desde|Period\s*From)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2})",
            ["EndDate"] =
                @"(?:To|Hasta|Period\s*To)[:\s]*(\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2})",

            // Balances
            ["OpeningBalance"] =
                @"(?:Opening\s*Balance|Balance\s*Inicial|Previous\s*Balance)[:\s]*\$?([0-9,]+\.?\d{0,2})",
            ["ClosingBalance"] =
                @"(?:Closing\s*Balance|Balance\s*Final|Current\s*Balance)[:\s]*\$?([0-9,]+\.?\d{0,2})",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(
                text,
                pattern.Value,
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );
            if (match.Success && match.Groups.Count > 1)
            {
                var value = match.Groups[1].Value.Trim();

                switch (pattern.Key)
                {
                    case "AccountNumber":
                        statement.AccountNumber = value;
                        break;

                    case "StatementDate":
                        if (TryParseDate(value, out var statementDate))
                            statement.StatementDate = statementDate;
                        break;

                    case "StartDate":
                        if (TryParseDate(value, out var startDate))
                            statement.StartDate = startDate;
                        break;

                    case "EndDate":
                        if (TryParseDate(value, out var endDate))
                            statement.EndDate = endDate;
                        break;

                    case "OpeningBalance":
                        if (TryParseAmount(value, out var openingBalance))
                            statement.OpeningBalance = openingBalance;
                        break;

                    case "ClosingBalance":
                        if (TryParseAmount(value, out var closingBalance))
                            statement.ClosingBalance = closingBalance;
                        break;
                }
            }
        }
    }

    private void ParseTransactions(BankStatement statement, string text)
    {
        // Patrones para diferentes formatos de transacciones en PDFs bancarios
        var transactionPatterns = new[]
        {
            // Formato: Fecha Descripción Monto
            @"(\d{1,2}[\/\-]\d{1,2}[\/\-](?:\d{2}|\d{4}))\s+([A-Za-z][^$\d\n]{10,50}?)\s+[\$]?([+-]?[0-9,]+\.?\d{0,2})",
            // Formato: Fecha Descripción Débito Crédito
            @"(\d{1,2}[\/\-]\d{1,2}[\/\-](?:\d{2}|\d{4}))\s+([A-Za-z][^$\d\n]{10,50}?)\s+(?:[\$]?([0-9,]+\.?\d{0,2}))?\s*(?:[\$]?([0-9,]+\.?\d{0,2}))?",
            // Formato con fecha al final: Descripción Monto Fecha
            @"([A-Za-z][^$\d\n]{10,50}?)\s+[\$]?([+-]?[0-9,]+\.?\d{0,2})\s+(\d{1,2}[\/\-]\d{1,2}[\/\-](?:\d{2}|\d{4}))",
            // Formato tabular simple
            @"(\d{1,2}[\/\-]\d{1,2})\s+([^$\d\n]{5,40})\s+([+-]?[0-9,]+\.?\d{0,2})",
        };

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var cleanLine = line.Trim();
            if (string.IsNullOrEmpty(cleanLine) || cleanLine.Length < 10)
                continue;

            foreach (var pattern in transactionPatterns)
            {
                var matches = Regex.Matches(cleanLine, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var transaction = ExtractTransactionFromMatch(match, pattern);
                        if (transaction != null && IsValidTransaction(transaction))
                        {
                            statement.Transactions.Add(transaction);
                            break; // Solo una transacción por línea
                        }
                    }
                }
            }
        }

        // Ordenar transacciones por fecha
        statement.Transactions = statement.Transactions.OrderBy(t => t.Date).ToList();
    }

    private Transaction? ExtractTransactionFromMatch(Match match, string pattern)
    {
        try
        {
            var transaction = new Transaction();

            // Determinar el orden de los grupos basado en el patrón
            if (pattern.StartsWith(@"(\d{1,2}[\/\-]\d{1,2}[\/\-](?:\d{2}|\d{4}))\s+([A-Za-z]"))
            {
                // Formato: Fecha Descripción Monto/Débito/Crédito
                if (TryParseDate(match.Groups[1].Value, out var date))
                    transaction.Date = date;

                transaction.Description = CleanDescription(match.Groups[2].Value);

                // Manejar débito/crédito o monto simple
                if (match.Groups.Count == 4) // Formato simple con un monto
                {
                    if (TryParseAmount(match.Groups[3].Value, out var amount))
                    {
                        transaction.Amount = Math.Abs(amount);
                        transaction.TransactionType = amount >= 0 ? "Credit" : "Debit";
                    }
                }
                else if (match.Groups.Count == 5) // Formato con débito y crédito separados
                {
                    var debitStr = match.Groups[3].Value;
                    var creditStr = match.Groups[4].Value;

                    if (
                        !string.IsNullOrEmpty(debitStr)
                        && TryParseAmount(debitStr, out var debitAmount)
                    )
                    {
                        transaction.Amount = debitAmount;
                        transaction.TransactionType = "Debit";
                    }
                    else if (
                        !string.IsNullOrEmpty(creditStr)
                        && TryParseAmount(creditStr, out var creditAmount)
                    )
                    {
                        transaction.Amount = creditAmount;
                        transaction.TransactionType = "Credit";
                    }
                }
            }
            else if (pattern.StartsWith(@"([A-Za-z]"))
            {
                // Formato: Descripción Monto Fecha
                transaction.Description = CleanDescription(match.Groups[1].Value);

                if (TryParseAmount(match.Groups[2].Value, out var amount))
                {
                    transaction.Amount = Math.Abs(amount);
                    transaction.TransactionType = amount >= 0 ? "Credit" : "Debit";
                }

                if (TryParseDate(match.Groups[3].Value, out var date))
                    transaction.Date = date;
            }

            return transaction;
        }
        catch
        {
            return null;
        }
    }

    private string CleanDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return "";

        // Limpiar la descripción eliminando caracteres extraños y espacios múltiples
        var cleaned = Regex.Replace(description, @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"[^\w\s\-\.\,]", "");
        return cleaned.Trim();
    }

    private bool IsValidTransaction(Transaction transaction)
    {
        return !string.IsNullOrEmpty(transaction.Description)
            && transaction.Amount > 0
            && transaction.Date != default
            && transaction.Date >= DateTime.Now.AddYears(-10)
            && // Transacciones no muy antiguas
            transaction.Date <= DateTime.Now.AddDays(1); // No en el futuro lejano
    }

    private bool TryParseDate(string dateStr, out DateTime date)
    {
        date = default;

        if (string.IsNullOrEmpty(dateStr))
            return false;

        // Formatos de fecha comunes
        var dateFormats = new[]
        {
            "MM/dd/yyyy",
            "M/d/yyyy",
            "MM/dd/yy",
            "M/d/yy",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd/MM/yy",
            "d/M/yy",
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "MM-dd-yyyy",
            "M-d-yyyy",
            "MM-dd-yy",
            "M-d-yy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "dd-MM-yy",
            "d-M-yy",
        };

        foreach (var format in dateFormats)
        {
            if (
                DateTime.TryParseExact(
                    dateStr.Trim(),
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date
                )
            )
            {
                // Si el año es de 2 dígitos y es menor que 50, asumimos que es 20xx
                if (date.Year < 1950)
                    date = date.AddYears(2000);
                else if (date.Year < 2000 && date.Year >= 50)
                    date = date.AddYears(1900);

                return true;
            }
        }

        // Intentar parse estándar como último recurso
        return DateTime.TryParse(dateStr, out date);
    }

    private bool TryParseAmount(string amountStr, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrEmpty(amountStr))
            return false;

        // Limpiar el string de caracteres no numéricos excepto punto, coma y signo negativo
        var cleaned = Regex.Replace(amountStr, @"[^\d\.\,\-\+]", "");

        // Manejar formatos con comas como separadores de miles
        if (cleaned.Contains(",") && cleaned.Contains("."))
        {
            // Formato: 1,234.56
            cleaned = cleaned.Replace(",", "");
        }
        else if (cleaned.Contains(",") && !cleaned.Contains("."))
        {
            // Podría ser formato europeo: 1234,56 o americano: 1,234
            var commaIndex = cleaned.LastIndexOf(',');
            if (commaIndex > 0 && cleaned.Length - commaIndex <= 3)
            {
                // Probablemente formato europeo: 1234,56
                cleaned = cleaned.Replace(",", ".");
            }
            else
            {
                // Probablemente separador de miles: 1,234
                cleaned = cleaned.Replace(",", "");
            }
        }

        return decimal.TryParse(
            cleaned,
            NumberStyles.Currency,
            CultureInfo.InvariantCulture,
            out amount
        );
    }

    private async Task<BankStatement> ParseXmlFileAsync(Stream fileStream)
    {
        var statement = new BankStatement
        {
            StatementDate = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            OpeningBalance = 0,
            ClosingBalance = 0,
            AccountNumber = "N/A",
        };

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(fileStream);

            // Parsear información básica del statement
            var accountNode =
                xmlDoc.SelectSingleNode("//AccountNumber") ?? xmlDoc.SelectSingleNode("//account");
            if (accountNode != null)
                statement.AccountNumber = accountNode.InnerText;

            var startDateNode =
                xmlDoc.SelectSingleNode("//StartDate") ?? xmlDoc.SelectSingleNode("//start_date");
            if (
                startDateNode != null
                && DateTime.TryParse(startDateNode.InnerText, out var startDate)
            )
                statement.StartDate = startDate;

            var endDateNode =
                xmlDoc.SelectSingleNode("//EndDate") ?? xmlDoc.SelectSingleNode("//end_date");
            if (endDateNode != null && DateTime.TryParse(endDateNode.InnerText, out var endDate))
                statement.EndDate = endDate;

            var openingBalanceNode =
                xmlDoc.SelectSingleNode("//OpeningBalance")
                ?? xmlDoc.SelectSingleNode("//opening_balance");
            if (
                openingBalanceNode != null
                && decimal.TryParse(openingBalanceNode.InnerText, out var openingBalance)
            )
                statement.OpeningBalance = openingBalance;

            var closingBalanceNode =
                xmlDoc.SelectSingleNode("//ClosingBalance")
                ?? xmlDoc.SelectSingleNode("//closing_balance");
            if (
                closingBalanceNode != null
                && decimal.TryParse(closingBalanceNode.InnerText, out var closingBalance)
            )
                statement.ClosingBalance = closingBalance;

            // Parsear transacciones
            var transactionNodes =
                xmlDoc.SelectNodes("//Transaction") ?? xmlDoc.SelectNodes("//transaction");
            if (transactionNodes != null)
            {
                foreach (XmlNode transactionNode in transactionNodes)
                {
                    var dateNode =
                        transactionNode.SelectSingleNode("Date")
                        ?? transactionNode.SelectSingleNode("date");
                    var descriptionNode =
                        transactionNode.SelectSingleNode("Description")
                        ?? transactionNode.SelectSingleNode("description");
                    var amountNode =
                        transactionNode.SelectSingleNode("Amount")
                        ?? transactionNode.SelectSingleNode("amount");
                    var typeNode =
                        transactionNode.SelectSingleNode("Type")
                        ?? transactionNode.SelectSingleNode("type");

                    if (dateNode != null && descriptionNode != null && amountNode != null)
                    {
                        if (
                            DateTime.TryParse(dateNode.InnerText, out var transactionDate)
                            && decimal.TryParse(amountNode.InnerText, out var amount)
                        )
                        {
                            var transaction = new Transaction
                            {
                                Date = transactionDate,
                                Description = descriptionNode.InnerText,
                                Amount = Math.Abs(amount),
                                TransactionType =
                                    typeNode?.InnerText ?? (amount >= 0 ? "Credit" : "Debit"),
                            };

                            statement.Transactions.Add(transaction);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new FormatException($"Error parsing XML file: {ex.Message}", ex);
        }

        return statement;
    }

    private async Task<BankStatement> ParseJsonFileAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8);
        var jsonContent = await reader.ReadToEndAsync();

        var statement = new BankStatement
        {
            StatementDate = DateTime.UtcNow,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            OpeningBalance = 0,
            ClosingBalance = 0,
            AccountNumber = "N/A",
        };

        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // Parsear información básica del statement
            if (
                root.TryGetProperty("accountNumber", out var accountElement)
                || root.TryGetProperty("AccountNumber", out accountElement)
            )
                statement.AccountNumber = accountElement.GetString() ?? "N/A";

            if (
                root.TryGetProperty("startDate", out var startDateElement)
                || root.TryGetProperty("StartDate", out startDateElement)
            )
            {
                if (DateTime.TryParse(startDateElement.GetString(), out var startDate))
                    statement.StartDate = startDate;
            }

            if (
                root.TryGetProperty("endDate", out var endDateElement)
                || root.TryGetProperty("EndDate", out endDateElement)
            )
            {
                if (DateTime.TryParse(endDateElement.GetString(), out var endDate))
                    statement.EndDate = endDate;
            }

            if (
                root.TryGetProperty("openingBalance", out var openingBalanceElement)
                || root.TryGetProperty("OpeningBalance", out openingBalanceElement)
            )
            {
                if (openingBalanceElement.TryGetDecimal(out var openingBalance))
                    statement.OpeningBalance = openingBalance;
            }

            if (
                root.TryGetProperty("closingBalance", out var closingBalanceElement)
                || root.TryGetProperty("ClosingBalance", out closingBalanceElement)
            )
            {
                if (closingBalanceElement.TryGetDecimal(out var closingBalance))
                    statement.ClosingBalance = closingBalance;
            }

            // Parsear transacciones
            if (
                root.TryGetProperty("transactions", out var transactionsElement)
                || root.TryGetProperty("Transactions", out transactionsElement)
            )
            {
                foreach (var transactionElement in transactionsElement.EnumerateArray())
                {
                    var transaction = new Transaction();

                    if (
                        transactionElement.TryGetProperty("date", out var dateElement)
                        || transactionElement.TryGetProperty("Date", out dateElement)
                    )
                    {
                        if (DateTime.TryParse(dateElement.GetString(), out var date))
                            transaction.Date = date;
                    }

                    if (
                        transactionElement.TryGetProperty("description", out var descElement)
                        || transactionElement.TryGetProperty("Description", out descElement)
                    )
                        transaction.Description = descElement.GetString() ?? "";

                    if (
                        transactionElement.TryGetProperty("amount", out var amountElement)
                        || transactionElement.TryGetProperty("Amount", out amountElement)
                    )
                    {
                        if (amountElement.TryGetDecimal(out var amount))
                        {
                            transaction.Amount = Math.Abs(amount);
                            transaction.TransactionType = amount >= 0 ? "Credit" : "Debit";
                        }
                    }

                    if (
                        transactionElement.TryGetProperty("type", out var typeElement)
                        || transactionElement.TryGetProperty("Type", out typeElement)
                    )
                        transaction.TransactionType =
                            typeElement.GetString() ?? transaction.TransactionType;

                    if (
                        transactionElement.TryGetProperty("category", out var categoryElement)
                        || transactionElement.TryGetProperty("Category", out categoryElement)
                    )
                        transaction.Category = categoryElement.GetString();

                    statement.Transactions.Add(transaction);
                }
            }
        }
        catch (Exception ex)
        {
            throw new FormatException($"Error parsing JSON file: {ex.Message}", ex);
        }

        return statement;
    }

    private async Task<BankStatement> ParseXlsxFileAsync(Stream fileStream)
    {
        // ClosedXML requiere un stream con seek.
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheets.First(); // primera hoja

        // ─── 1. Detectar la fila de cabecera ──────────────────────────────
        int headerRow = -1;
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int row = 1; row <= 30; row++) // busca en las 30 primeras filas
        {
            headerMap.Clear();

            foreach (var cell in ws.Row(row).CellsUsed())
            {
                var text = cell.GetString().Trim().ToLowerInvariant();

                if (text.Contains("date") || text.Contains("fecha"))
                    headerMap["Date"] = cell.Address.ColumnNumber;

                if (
                    text.Contains("description")
                    || text.Contains("concepto")
                    || text.Contains("detalle")
                    || text.Contains("concept")
                )
                    headerMap["Desc"] = cell.Address.ColumnNumber;

                if (
                    text.Contains("amount")
                    || text.Contains("importe")
                    || text.Contains("monto")
                    || text.Contains("valor")
                )
                    headerMap["Amount"] = cell.Address.ColumnNumber;

                if (text.Contains("debit") || text.Contains("débito") || text.Contains("withdraw"))
                    headerMap["Debit"] = cell.Address.ColumnNumber;

                if (text.Contains("credit") || text.Contains("crédito") || text.Contains("deposit"))
                    headerMap["Credit"] = cell.Address.ColumnNumber;

                if (text.Contains("categor"))
                    headerMap["Cat"] = cell.Address.ColumnNumber;
            }

            // Condición válida: (Date y Desc) y (Amount ó (Debit+Credit))
            bool ok =
                headerMap.ContainsKey("Date")
                && headerMap.ContainsKey("Desc")
                && (
                    headerMap.ContainsKey("Amount")
                    || (headerMap.ContainsKey("Debit") && headerMap.ContainsKey("Credit"))
                );

            if (ok)
            {
                headerRow = row;
                break;
            }
        }

        if (headerRow == -1)
            throw new FormatException("No se encontró una cabecera válida en el XLSX.");

        // ─── 2. Crear el statement ────────────────────────────────────────
        var statement = new BankStatement
        {
            AccountNumber = Path.GetFileNameWithoutExtension("XLSX_UPLOAD"),
            StatementDate = DateTime.UtcNow,
        };

        // ─── 3. Leer filas de datos ───────────────────────────────────────
        for (int r = headerRow + 1; !ws.Row(r).IsEmpty(); r++)
        {
            // 3.1 Fecha
            var dateCell = ws.Row(r).Cell(headerMap["Date"]);
            if (!TryDateFromCell(dateCell, out var txDate))
                continue;

            // 3.2 Descripción
            var descCell = ws.Row(r).Cell(headerMap["Desc"]);
            var desc = descCell.GetString().Trim();
            if (desc.Length == 0)
                continue;

            // 3.3 Monto
            decimal amount;
            if (headerMap.ContainsKey("Amount"))
            {
                var amtCell = ws.Row(r).Cell(headerMap["Amount"]);
                if (!TryDecimalFromCell(amtCell, out amount))
                    continue;
            }
            else
            {
                var debCell = ws.Row(r).Cell(headerMap["Debit"]);
                var creCell = ws.Row(r).Cell(headerMap["Credit"]);

                if (TryDecimalFromCell(debCell, out var deb) && deb != 0)
                    amount = -deb;
                else if (TryDecimalFromCell(creCell, out var cre) && cre != 0)
                    amount = cre;
                else
                    continue; // ambos vacíos → salta fila
            }

            // 3.4 Categoría (opcional)
            string? cat = null;
            if (headerMap.ContainsKey("Cat"))
                cat = ws.Row(r).Cell(headerMap["Cat"]).GetString().Trim();

            // 3.5 Agregar transacción
            statement.Transactions.Add(
                new Transaction
                {
                    Date = txDate,
                    Description = desc,
                    Amount = Math.Abs(amount),
                    TransactionType = amount >= 0 ? "Credit" : "Debit",
                    Category = cat,
                }
            );
        }

        // ─── 4. Rango de fechas ───────────────────────────────────────────
        if (statement.Transactions.Any())
        {
            statement.StartDate = statement.Transactions.Min(t => t.Date);
            statement.EndDate = statement.Transactions.Max(t => t.Date);
        }

        return statement;
    }

    // -------------------- helpers locales --------------------
    private static bool TryDateFromCell(IXLCell cell, out DateTime date)
    {
        if (cell.DataType == XLDataType.DateTime)
        {
            date = cell.GetDateTime();
            return true;
        }
        return TryParseDateEnhanced(cell.GetString(), out date);
    }

    private static bool TryDecimalFromCell(IXLCell cell, out decimal value)
    {
        value = 0;
        if (cell.DataType == XLDataType.Number)
        {
            value = (decimal)cell.GetDouble();
            return true;
        }
        var txt = cell.GetString().Trim().Replace(",", "");
        return decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private DateTime ParseOfxDate(string ofxDate)
    {
        // Formato OFX: YYYYMMDDHHMMSS o YYYYMMDD
        if (
            ofxDate.Length >= 8
            && DateTime.TryParseExact(
                ofxDate.Substring(0, 8),
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date
            )
        )
        {
            return date;
        }
        return DateTime.UtcNow;
    }
}
