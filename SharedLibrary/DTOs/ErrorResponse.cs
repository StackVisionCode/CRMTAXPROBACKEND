namespace SharedLibrary.DTOs;

public class errorResponse
{
    public string Service { get; set; } = "UnknownService";
    public int StatusCode { get; set; }
    public string Message { get; set; } = "An unexpected error occurred.";
    public string Error { get; set; } = string.Empty;
    public string? Path { get; set; }
}