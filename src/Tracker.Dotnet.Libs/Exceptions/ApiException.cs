namespace Tracker.Dotnet.Libs.Exceptions;

/// <summary>
/// Exception for explicit throwing, must return 400 Bad request.
/// </summary>
public class ApiException : Exception
{
    public string[] Details { get; }

    public ApiException(string message) : base(message)
    {
        Details = new[] { message };
    }

    public ApiException(string message, IEnumerable<string> details) : base(message)
    {
        Details = details.ToArray();
    }
}
