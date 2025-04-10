namespace Tracker.Dotnet.Auth.Models
{
    public class Result<T>
    {
        public Result(T value)
        {
            Success = true;
            Value = value;
        }

        public Result(int errorCode, string message)
        {
            Success = false;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool Success { get; }

        public int? ErrorCode { get; }

        public string? Message { get; }

        public T? Value { get; }
    }
}
