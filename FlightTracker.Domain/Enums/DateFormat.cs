namespace FlightTracker.Domain.Enums;

/// <summary>
/// Date format preference for displaying dates.
/// </summary>
public enum DateFormat
{
    /// <summary>
    /// DD/MM/YYYY (e.g., 25/12/2025)
    /// </summary>
    DayMonthYear = 0,
    
    /// <summary>
    /// MM/DD/YYYY (e.g., 12/25/2025)
    /// </summary>
    MonthDayYear = 1,
    
    /// <summary>
    /// YYYY-MM-DD (e.g., 2025-12-25) - ISO 8601
    /// </summary>
    YearMonthDay = 2
}
