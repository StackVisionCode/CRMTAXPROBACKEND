using System.Text.Json;
using Application.Common;
using Infrastructure.Services;

namespace Infrastructure.Services;
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private const string UPLOAD_FOLDER = "uploads";
    private const string FORMS_FOLDER = "forms";

    public FileService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<ApiResponse<bool>> UploadFileAsync(IFormFile file, string formInstanceId, string fieldName)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<bool>(false, "No file provided.");
            }

            // Validar tamaño de archivo (ejemplo: 10MB máximo)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return new ApiResponse<bool>(false, "File size exceeds the maximum allowed size (10MB).");
            }

            // Validar extensión de archivo
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".csv", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new ApiResponse<bool>(false, $"File type '{fileExtension}' is not allowed.");
            }

            // Crear estructura de carpetas: uploads/forms/{formInstanceId}/{fieldName}/
            var uploadPath = Path.Combine(_environment.WebRootPath, UPLOAD_FOLDER, FORMS_FOLDER, formInstanceId, fieldName);
            Directory.CreateDirectory(uploadPath);

            // Generar nombre único para el archivo
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Guardar el archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornar la ruta relativa para almacenar en BD
            var relativePath = Path.Combine(UPLOAD_FOLDER, FORMS_FOLDER, formInstanceId, fieldName, uniqueFileName)
                                   .Replace("\\", "/"); // Normalizar para web

            return new ApiResponse<bool>(true, "File uploaded successfully.");
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error uploading file: {ex.Message}");
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, filePath.Replace("/", "\\"));
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string filePath)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:5172";
        return $"{baseUrl.TrimEnd('/')}/{filePath}";
    }

    public async Task<ApiResponse<List<string>>> ProcessFormFilesAsync(string formData, string formInstanceId)
    {
        try
        {
            var processedFiles = new List<string>();
            
            // Parsear el JSON de datos del formulario
            var formDataJson = JsonDocument.Parse(formData);
            var root = formDataJson.RootElement;

            // Procesar cada campo del formulario
            foreach (var field in root.EnumerateObject())
            {
                if (field.Name == "_metadata") continue; // Saltar metadatos

                var fieldValue = field.Value;
                
                // Verificar si es un archivo o array de archivos
                if (fieldValue.ValueKind == JsonValueKind.Object && HasFileProperties(fieldValue))
                {
                    // Archivo individual
                    var fileName = fieldValue.GetProperty("name").GetString();
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        processedFiles.Add($"Field '{field.Name}': {fileName}");
                    }
                }
                else if (fieldValue.ValueKind == JsonValueKind.Array)
                {
                    // Array de archivos
                    foreach (var item in fieldValue.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object && HasFileProperties(item))
                        {
                            var fileName = item.GetProperty("name").GetString();
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                processedFiles.Add($"Field '{field.Name}': {fileName}");
                            }
                        }
                    }
                }
            }

            return new ApiResponse<List<string>>("Files processed successfully.",processedFiles);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<string>>(false, $"Error processing files: {ex.Message}");
        }
    }

    private static bool HasFileProperties(JsonElement element)
    {
        return element.TryGetProperty("name", out _) && 
               element.TryGetProperty("size", out _) && 
               element.TryGetProperty("type", out _);
    }
}
