using System.Collections.Generic;

namespace FlightTracker.Application.Results;

/// <summary>
/// Represents a paginated operation result with metadata.
/// </summary>
public sealed class PaginatedResult<T> : Result<IReadOnlyList<T>>
{
    private PaginatedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
        : base(items)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    private PaginatedResult(string errorMessage, string? errorCode)
        : base(errorMessage, errorCode)
    {
        PageNumber = 1;
        PageSize = 0;
        TotalCount = 0;
    }

    public int PageNumber { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages =>
        PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PaginatedResult<T> Success(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new PaginatedResult<T>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }

    public static new PaginatedResult<T> Failure(
        string errorMessage,
        string? errorCode = null)
    {
        return new PaginatedResult<T>(errorMessage, errorCode);
    }
}
