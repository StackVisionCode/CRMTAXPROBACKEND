using System.Reflection.Metadata;
using Application.Interfaes;
using BankStaments.Domain.Tbl_IA_Entity;

namespace Infrastructure.Services;

public class DeepSeekService : IDeepSeekService
{ private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IStorageService _storageService;
    private readonly ILogger<DeepSeekService> _logger;

    public DeepSeekService(
        HttpClient httpClient, 
        IConfiguration configuration,
        IStorageService storageService,
        ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _storageService = storageService;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_configuration["DeepSeek:ApiBaseUrl"]!);
        _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("DeepSeek:Timeout"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration["DeepSeek:ApiKey"]}");
    }

    public async Task<DeepSeekApiResponse> ProcessBankDocumentAsync(
        Stream fileStream, string fileName, string accountNumber, Guid customerId)
    {
        try
        {
            // 1. Subir el documento a almacenamiento temporal
            var fileUrl = await _storageService.UploadFileAsync(fileStream, fileName);

            // 2. Preparar la solicitud a DeepSeek
            var request = new DeepSeekApiRequest
            {
                DocumentUrl = fileUrl,
                DocumentType = "bank_statement",
                Metadata = new Dictionary<string, string>
                {
                    { "account_number", accountNumber },
                    { "customer_id", customerId.ToString() }
                }
            };

            // 3. Enviar a la API de DeepSeek
            var response = await _httpClient.PostAsJsonAsync("/documents/process", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error al procesar documento: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                throw new HttpRequestException($"Error en la API DeepSeek: {response.StatusCode}");
            }

            var DocuemntViewed = await response.Content.ReadFromJsonAsync<DeepSeekApiResponse>();
            if (DocuemntViewed == null)
            {
                return null!;
            }
            return DocuemntViewed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar documento bancario");
            throw;
        }
    }

    public async Task<DeepSeekApiResponse> GetProcessingStatusAsync(string requestId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/documents/status/{requestId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error al obtener estado: {response.StatusCode}");
                throw new HttpRequestException($"Error en la API DeepSeek: {response.StatusCode}");
            }

          //  return await response.Content.ReadFromJsonAsync<DeepSeekApiResponse>();

                  var DocuemntViewed = await response.Content.ReadFromJsonAsync<DeepSeekApiResponse>();
            if (DocuemntViewed == null)
            {
                return null!;
            }
            return DocuemntViewed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de procesamiento");
            throw;
        }
    }
}