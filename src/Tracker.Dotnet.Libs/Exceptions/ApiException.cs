namespace Tracker.Dotnet.Libs.Exceptions;

/// <summary>
/// Exception for explicit throwing, must return 400 Bad request.
/// </summary>
public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }
}
