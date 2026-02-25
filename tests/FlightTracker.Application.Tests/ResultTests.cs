using FlightTracker.Application.Results;
using Xunit;

namespace FlightTracker.Application.Tests.Services;

public class ResultTests
{
    [Fact]
    public void Success_WithoutValue_IsSuccessful()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_WithoutMessage_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => Result.Failure(string.Empty));
    }

    [Fact]
    public void GenericSuccess_WithValue_ReturnsValue()
    {
        var result = Result<string>.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public void PaginatedSuccess_ComputesTotalPages()
    {
        var result = PaginatedResult<int>.Success(
            new[] { 1, 2, 3 },
            pageNumber: 2,
            pageSize: 3,
            totalCount: 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(3, result.Value?.Count);
    }
}
