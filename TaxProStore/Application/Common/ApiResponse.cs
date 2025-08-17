namespace Application.Common;

public class ApiResponse<T>
{
    public bool? Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public ApiResponse() { }

    public int? StatusCode { get; set; }

    public ApiResponse(bool success, string message, T data)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public ApiResponse(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public ApiResponse(string message, T data)
    {
        Message = message;
        Data = data;
    }

    public ApiResponse(bool success)
    {
        Success = success;
    }

    
    public ApiResponse(bool success, T data)
    {
        Success = success;
        Data = data;
    }
    

    public ApiResponse(bool success, string message, T data, int? statusCode = null)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }
}
