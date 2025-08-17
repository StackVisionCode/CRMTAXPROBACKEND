using Application.Common;

namespace Infrastructure.Services;

public interface IFileService
{
    Task<ApiResponse<bool>> UploadFileAsync(IFormFile file, string formInstanceId, string fieldName);
    Task<bool> DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
    Task<ApiResponse<List<string>>> ProcessFormFilesAsync(string formData, string formInstanceId);
}