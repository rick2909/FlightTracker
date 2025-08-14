using FlightTracker.Application.Dtos;

namespace FlightTracker.Web.Models.ViewModels;

/// <summary>
/// View model for the main dashboard page.
/// Contains user flight statistics and recent flight activity.
/// </summary>
public class DashboardViewModel
{
    /// <summary>
    /// User flight statistics (total flights, classes, etc.)
    /// </summary>
    public UserFlightStatsDto Stats { get; set; } = new();

    /// <summary>
    /// Recent user flights to display on dashboard.
    /// </summary>
    public IEnumerable<UserFlightDto> RecentFlights { get; set; } = new List<UserFlightDto>();

    /// <summary>
    /// Current user ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Error message to display if data loading fails.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Flights (past + upcoming) formatted for map (origin/destination pairs).
    /// </summary>
    public IEnumerable<MapFlightDto> MapFlights { get; set; } = Array.Empty<MapFlightDto>();

    /// <summary>
    /// Indicates if user has any flight data.
    /// </summary>
    public bool HasFlights => Stats.TotalFlights > 0;

    /// <summary>
    /// Quick stats for dashboard cards.
    /// </summary>
    public class QuickStats
    {
        public int TotalFlights => Stats?.TotalFlights ?? 0;
        public int UniqueAirports => Stats?.UniqueAirports ?? 0;
        public int UniqueCountries => Stats?.UniqueCountries ?? 0;
        public string MostUsedClass => GetMostUsedClass();

        private readonly UserFlightStatsDto? Stats;

        public QuickStats(UserFlightStatsDto stats)
        {
            Stats = stats;
        }

        private string GetMostUsedClass()
        {
            if (Stats == null) return "N/A";

            var max = Math.Max(
                Math.Max(Stats.EconomyFlights, Stats.PremiumEconomyFlights),
                Math.Max(Stats.BusinessFlights, Stats.FirstClassFlights)
            );

            if (max == 0) return "N/A";

            if (Stats.FirstClassFlights == max) return "First";
            if (Stats.BusinessFlights == max) return "Business";
            if (Stats.PremiumEconomyFlights == max) return "Premium Economy";
            return "Economy";
        }
    }

    /// <summary>
    /// Gets quick stats for the dashboard.
    /// </summary>
    public QuickStats GetQuickStats() => new(Stats);
}
