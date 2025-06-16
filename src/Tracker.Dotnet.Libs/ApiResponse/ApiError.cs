namespace Tracker.Dotnet.Libs.ApiResponse;

public class ApiError
{
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string>? Details { get; set; }
}
