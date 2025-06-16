namespace Tracker.Dotnet.Libs.ApiResponse;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ApiError? Error { get; set; }

    public static ApiResponse<T> Success(T data) => new() { Data = data };
    public static ApiResponse<T> Fail(string message, IEnumerable<string>? details = null) =>
        new() { Error = new ApiError { Message = message, Details = details } };
}
