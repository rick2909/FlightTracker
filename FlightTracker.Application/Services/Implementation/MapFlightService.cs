using FlightTracker.Application.Dtos;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Provides flight data formatted for map visualization (simple origin-destination pairs).
/// </summary>
public class MapFlightService : IMapFlightService
{
    private readonly IUserFlightRepository _userFlightRepository;

    public MapFlightService(IUserFlightRepository userFlightRepository)
    {
        _userFlightRepository = userFlightRepository;
    }

    public async Task<IReadOnlyCollection<MapFlightDto>> GetUserMapFlightsAsync(int userId, int maxPast = 20, int maxUpcoming = 10, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        var projected = userFlights
            .Where(uf => uf.Flight != null && uf.Flight.DepartureAirport != null && uf.Flight.ArrivalAirport != null)
            .Select(uf => new MapFlightDto
            {
                FlightId = uf.FlightId,
                FlightNumber = uf.Flight!.FlightNumber,
                DepartureTimeUtc = uf.Flight.DepartureTimeUtc,
                ArrivalTimeUtc = uf.Flight.ArrivalTimeUtc,
                DepartureAirportCode = uf.Flight.DepartureAirport!.Code,
                ArrivalAirportCode = uf.Flight.ArrivalAirport!.Code,
                DepartureLat = uf.Flight.DepartureAirport.Latitude,
                DepartureLon = uf.Flight.DepartureAirport.Longitude,
                ArrivalLat = uf.Flight.ArrivalAirport.Latitude,
                ArrivalLon = uf.Flight.ArrivalAirport.Longitude,
                IsUpcoming = uf.Flight.DepartureTimeUtc >= now
            })
            .ToList();

        var upcoming = projected
            .Where(f => f.IsUpcoming)
            .OrderBy(f => f.DepartureTimeUtc)
            .Take(maxUpcoming);

        var past = projected
            .Where(f => !f.IsUpcoming)
            .OrderByDescending(f => f.DepartureTimeUtc)
            .Take(maxPast);

        return past.Concat(upcoming).ToList();
    }
}
