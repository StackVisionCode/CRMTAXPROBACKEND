namespace Application.Interfaes;


public interface IStorageService
{
  Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<Stream> DownloadFileAsync(string fileUrl);
    Task DeleteFile(string fileUrl);
}
