
    public class ApiResponse<T>
    {
    public bool _Success { get; set; }
   
    public string? Message { get; set; }
    public T? Data { get; set; }

    
    public ApiResponse(bool success, string? message = null, T? data = default)
    {
        _Success = success;
        Message = message;
        Data = data;
    }

    
    public static ApiResponse<T> Success(T data, string? message = null)
    {
        return new ApiResponse<T>(true, message, data);
    }

    
    public static ApiResponse<T> Failure(string message)
    {
        return new ApiResponse<T>(false, message);
    }
}
    

