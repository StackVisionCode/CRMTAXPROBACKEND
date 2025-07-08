// ChatPDFController.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace BankStaments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatPDFController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ChatPDFController(IConfiguration config)
        {
            _config = config;
        }

        // ───────────────────────────────────────── ENDPOINT PRINCIPAL
        [HttpPost("AnalyzeWithChatPdf")]
        public async Task<IActionResult> AnalyzeWithChatPdf([FromForm] BankStatementUploadDto file)
        {
            if (file == null || file.File.Length == 0)
                return BadRequest("Archivo no válido.");

            try
            {
                // 1. Subir PDF y obtener sourceId
                var sourceId = await UploadPdfToChatPdfAsync(file.File);

                // 2. Prompt que obliga a ChatPDF a devolver JSON con categorías en los gastos
                var prompt = @"
You are a financial data extraction assistant. Carefully read the entire bank statement, regardless of the language it is written in (e.g., English, Spanish, etc.). Your task is to extract and return the following data in English, in a valid JSON format (no explanations, no markdown):

{
  ""beginningBalance"": number,
  ""endingBalance"": number,
  ""expenses"": [
    { ""date"": ""string"", ""description"": ""string"", ""amount"": number, ""category"": ""string"" }
  ],
  ""income"": [
    { ""date"": ""string"", ""description"": ""string"", ""amount"": number }
  ],
  ""summaryTotals"": {
    ""totalExpenses"": number,
    ""totalIncome"": number
  },
  ""dailyBalances"": [
    {
      ""date"": ""MM/DD"",
      ""checkNumber"": ""string"",
      ""description"": ""string"",
      ""depositOrAddition"": number,
      ""withdrawalOrDeduction"": number,
      ""finalDailyBalance"": number
    }
  ]
}

Categorize each expense based on its description using only one of the following categories (do not invent new ones):

""Fuel"", ""Groceries"", ""Dining"", ""Utilities"", ""Rent/Mortgage"", ""Entertainment"", ""Transport"", ""Healthcare"", ""Travel"", ""Education"", ""Insurance"", ""Clothing"", ""PersonalCare"", ""Gifts"", ""Charity"", ""Subscriptions"", ""PetCare"", ""Childcare"", ""Investments"", ""Taxes"", ""Fees"", ""Maintenance"", ""Electronics"", ""OfficeSupplies"", ""Hardware"", ""Software"", ""Legal"", ""ProfessionalServices"", ""Advertising"", ""Marketing"", ""Consulting"", ""Training"", ""HomeImprovement"", ""Furniture"", ""Appliances"", ""Gardening"", ""Sports"", ""Fitness"", ""Alcohol"", ""Tobacco"", ""Telecommunications"", ""BankCharges"", ""LoanPayments"", ""Interest"", ""Savings"", ""Retirement"", ""Events"", ""Books"", ""Music"", ""Art"", ""Photography"", ""Jewelry"", ""Beauty"", ""AutoInsurance"", ""PropertyInsurance"", ""LoanInterest"", ""MortgageInterest"", ""Repairs"", ""Warranty"", ""Other""

Even if the document is written in a different language, extract all relevant data and return the final result in English as a JSON object. If any value is missing or unclear, use `null` or leave the field empty, but preserve the structure.";

                // 3. Llamar a ChatPDF
                var jsonResponse = await AskChatPdfAsync(sourceId, prompt);

                // 4. Deserializar a nuestro modelo fuertemente tipado
                var result = JsonSerializer.Deserialize<StatementAnalysisResult>(
                    jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result == null)
                    return StatusCode(500, "No se pudo analizar el JSON de respuesta.");

                return Ok(result);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine("Error al deserializar JSON: " + jsonEx.Message);
                return StatusCode(500, "El formato de respuesta no es válido.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "Error al procesar el PDF: " + ex.Message);
            }
        }

        // ───────────────────────────────────────── Subir PDF
        private async Task<string> UploadPdfToChatPdfAsync(IFormFile file)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", _config["ChatPDF:ApiKey"]);

            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync("https://api.chatpdf.com/v1/sources/add-file", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ChatPdfUploadResponse>(responseContent);

            return result?.SourceId ?? throw new Exception("No se pudo obtener el Source ID.");
        }

        // ───────────────────────────────────────── Preguntar a ChatPDF
        private async Task<string> AskChatPdfAsync(string sourceId, string prompt)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", _config["ChatPDF:ApiKey"]);

            var body = new
            {
                sourceId,
                referenceSources = false,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.chatpdf.com/v1/chats/message", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ChatPdfChatResponse>(responseContent);

            return result?.Content ?? throw new Exception("No se pudo obtener respuesta válida de ChatPDF.");
        }

        // ───────────────────────────────────────── MODELOS

        public class BankStatementUploadDto
        {
            [FromForm(Name = "file")]
            public IFormFile File { get; set; } = null!;
        }

        public class ChatPdfUploadResponse
        {
            [JsonPropertyName("sourceId")]
            public string SourceId { get; set; } = string.Empty;
        }

        public class ChatPdfChatResponse
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        public class StatementAnalysisResult
        {
            [JsonPropertyName("beginningBalance")]
            public decimal? BeginningBalance { get; set; }

            [JsonPropertyName("endingBalance")]
            public decimal? EndingBalance { get; set; }

            [JsonPropertyName("expenses")]
            public List<ExpenseTransaction> Expenses { get; set; } = new();

            [JsonPropertyName("income")]
            public List<IncomeTransaction> Income { get; set; } = new();

            [JsonPropertyName("summaryTotals")]
            public SummaryTotals SummaryTotals { get; set; } = new();
        }

        // Gastos (con categoría)
        public class ExpenseTransaction
        {
            [JsonPropertyName("date")]
            public string Date { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; } = "Other";
        }

        // Ingresos (sin categoría)
        public class IncomeTransaction
        {
            [JsonPropertyName("date")]
            public string Date { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string Description { get; set; } = string.Empty;

            [JsonPropertyName("amount")]
            public decimal Amount { get; set; }
        }

        public class SummaryTotals
        {
            [JsonPropertyName("totalExpenses")]
            public decimal TotalExpenses { get; set; }

            [JsonPropertyName("totalIncome")]
            public decimal TotalIncome { get; set; }
        }
    }
}
