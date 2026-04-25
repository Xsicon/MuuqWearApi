namespace MuuqWear.API.Shared;
public class Response<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Response<T> SuccessResponse(T data, string message = "")
    {
        return new Response<T>
        {
            Data = data,
            Success = true,
            Message = message
        };
    }

    public static Response<T> Fail(string message)
    {
        return new Response<T>
        {
            Success = false,
            Message = message
        };
    }
}
