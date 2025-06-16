namespace Tracker.Dotnet.Libs.Exceptions;

/// <summary>
/// Exception for explicit throwing when authentication was failed, must return 401 Unauthorized
/// </summary>
public class WrongCredentialsException : Exception
{
}
