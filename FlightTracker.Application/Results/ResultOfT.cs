namespace FlightTracker.Application.Results;

/// <summary>
/// Represents the typed outcome of an operation.
/// </summary>
public class Result<T> : Result
{
    protected Result(T? value)
        : base(true, null, null)
    {
        Value = value;
    }

    protected Result(string errorMessage, string? errorCode)
        : base(false, errorCode, errorMessage)
    {
        Value = default;
    }

    public T? Value { get; }

    public static Result<T> Success(T? value)
    {
        return new Result<T>(value);
    }

    public static new Result<T> Failure(
        string errorMessage,
        string? errorCode = null)
    {
        return new Result<T>(errorMessage, errorCode);
    }
}
