namespace FlightTracker.Application.Results;

/// <summary>
/// Represents the outcome of an operation.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? errorCode, string? errorMessage)
    {
        if (isSuccess && errorMessage is not null)
        {
            throw new ArgumentException(
                "Successful result cannot contain an error message.",
                nameof(errorMessage));
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException(
                "Failed result must contain an error message.",
                nameof(errorMessage));
        }

        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static Result Success()
    {
        return new Result(true, null, null);
    }

    public static Result Failure(
        string errorMessage,
        string? errorCode = null)
    {
        return new Result(false, errorCode, errorMessage);
    }
}
