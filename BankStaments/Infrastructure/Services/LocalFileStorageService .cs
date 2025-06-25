using Application.Interfaes;

namespace BankStaments.Infrastructure.Services;

public class LocalFileStorageService : IStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _storagePath = configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "FileStorage");
        _logger = logger;

        // Asegurarse de que el directorio existe
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        try
        {

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".xlsx", ".xls", ".csv" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Tipo de archivo no permitido");
            }
            // Generar un nombre de archivo único
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);

            // Guardar el archivo
            using (var file = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(file);
            }

            // Retornar la ruta relativa o URL
            return $"/files/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir archivo");
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string fileUrl)
    {
        try
        {
            // Extraer el nombre del archivo de la URL
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Archivo no encontrado", filePath);
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar archivo");
            throw;
        }
    }

    public async Task DeleteFile(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(fileUrl);
            var filePath = Path.Combine(_storagePath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar archivo");
            throw;
        }
    }
}