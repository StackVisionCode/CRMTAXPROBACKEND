namespace Application.Helpers;

public class ApiResponse<T>
{
    public bool? Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

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

    // Constructor adicional para solo mensaje de error
    public ApiResponse(string message)
    {
        Success = false;
        Message = message;
    }

    // Métodos estáticos para crear instancias de manera conveniente
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>(true, null!, data);
    }

    public static ApiResponse<T> Ok(T data, string message)
    {
        return new ApiResponse<T>(true, message, data);
    }

    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>(false, message);
    }

    public static ApiResponse<T> Fail(string message, T data)
    {
        return new ApiResponse<T>(false, message, data);
    }
}
