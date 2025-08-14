using FlightTracker.Application.Dtos;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using FlightTracker.Application.Repositories.Interfaces;

namespace FlightTracker.Application.Services.Implementation;

/// <summary>
/// Service implementation for managing user flight experiences.
/// </summary>
public class UserFlightService : IUserFlightService
{
    private readonly IUserFlightRepository _userFlightRepository;
    private readonly IFlightRepository _flightRepository;

    public UserFlightService(
        IUserFlightRepository userFlightRepository,
        IFlightRepository flightRepository)
    {
        _userFlightRepository = userFlightRepository;
        _flightRepository = flightRepository;
    }

    public async Task<IEnumerable<UserFlightDto>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsAsync(userId, cancellationToken);
        return userFlights.Select(MapToDto);
    }

    public async Task<UserFlightDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var userFlight = await _userFlightRepository.GetByIdAsync(id, cancellationToken);
        return userFlight != null ? MapToDto(userFlight) : null;
    }

    public async Task<IEnumerable<UserFlightDto>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsByClassAsync(userId, flightClass, cancellationToken);
        return userFlights.Select(MapToDto);
    }

    public async Task<UserFlightDto> AddUserFlightAsync(int userId, CreateUserFlightDto createDto, CancellationToken cancellationToken = default)
    {
        // Validate that the flight exists
        var flight = await _flightRepository.GetByIdAsync(createDto.FlightId, cancellationToken);
        if (flight == null)
        {
            throw new ArgumentException($"Flight with ID {createDto.FlightId} not found.", nameof(createDto.FlightId));
        }

        // Check if user has already recorded this flight
        var hasFlown = await _userFlightRepository.HasUserFlownFlightAsync(userId, createDto.FlightId, cancellationToken);
        if (hasFlown)
        {
            throw new InvalidOperationException($"User {userId} has already recorded flight {createDto.FlightId}.");
        }

        var userFlight = new UserFlight
        {
            UserId = userId,
            FlightId = createDto.FlightId,
            FlightClass = createDto.FlightClass,
            SeatNumber = createDto.SeatNumber,
            Notes = createDto.Notes,
            DidFly = createDto.DidFly,
            BookedOnUtc = DateTime.UtcNow
        };

        var savedUserFlight = await _userFlightRepository.AddAsync(userFlight, cancellationToken);
        
        // Reload to get navigation properties
        var reloadedUserFlight = await _userFlightRepository.GetByIdAsync(savedUserFlight.Id, cancellationToken);
        return MapToDto(reloadedUserFlight!);
    }

    public async Task<UserFlightDto?> UpdateUserFlightAsync(int id, CreateUserFlightDto updateDto, CancellationToken cancellationToken = default)
    {
        var existingUserFlight = await _userFlightRepository.GetByIdAsync(id, cancellationToken);
        if (existingUserFlight == null)
        {
            return null;
        }

        // Update properties
        existingUserFlight.FlightClass = updateDto.FlightClass;
        existingUserFlight.SeatNumber = updateDto.SeatNumber;
        existingUserFlight.Notes = updateDto.Notes;
        existingUserFlight.DidFly = updateDto.DidFly;

        var updatedUserFlight = await _userFlightRepository.UpdateAsync(existingUserFlight, cancellationToken);
        return MapToDto(updatedUserFlight);
    }

    public async Task<bool> DeleteUserFlightAsync(int id, CancellationToken cancellationToken = default)
    {
        var existingUserFlight = await _userFlightRepository.GetByIdAsync(id, cancellationToken);
        if (existingUserFlight == null)
        {
            return false;
        }

        await _userFlightRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<UserFlightStatsDto> GetUserFlightStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsAsync(userId, cancellationToken);
        var flownFlights = userFlights.Where(uf => uf.DidFly).ToList();

        var stats = new UserFlightStatsDto
        {
            UserId = userId,
            TotalFlights = flownFlights.Count,
            EconomyFlights = flownFlights.Count(uf => uf.FlightClass == FlightClass.Economy),
            PremiumEconomyFlights = flownFlights.Count(uf => uf.FlightClass == FlightClass.PremiumEconomy),
            BusinessFlights = flownFlights.Count(uf => uf.FlightClass == FlightClass.Business),
            FirstClassFlights = flownFlights.Count(uf => uf.FlightClass == FlightClass.First),
            UniqueAirports = flownFlights
                .SelectMany(uf => new[] { uf.Flight?.DepartureAirport?.Id, uf.Flight?.ArrivalAirport?.Id })
                .Where(id => id.HasValue)
                .Distinct()
                .Count(),
            UniqueCountries = flownFlights
                .SelectMany(uf => new[] { uf.Flight?.DepartureAirport?.Country, uf.Flight?.ArrivalAirport?.Country })
                .Where(country => !string.IsNullOrEmpty(country))
                .Distinct()
                .Count(),
            TotalTravelTimeInMinutes = flownFlights
                .Where(uf => uf.DidFly)
                .Sum(uf => GetFlightTimeInMinutes(uf)),
            TravelTimes = Enum
                .GetValues(typeof(FlightClass))
                .Cast<FlightClass>()
                .Select(fc => new TravelTimeDto
                {
                    FlightClass = fc,
                    TotalTravelTimeInMinutes = flownFlights
                        .Where(uf => uf.FlightClass == fc)
                        .Sum(uf => GetFlightTimeInMinutes(uf))
                })
                .ToList()
        };

        return stats;
    }

    public async Task<bool> HasUserFlownFlightAsync(int userId, int flightId, CancellationToken cancellationToken = default)
    {
        return await _userFlightRepository.HasUserFlownFlightAsync(userId, flightId, cancellationToken);
    }

    private int GetFlightTimeInMinutes(UserFlight userFlight)
    {
        var DepartureTimeUtc = userFlight.Flight?.DepartureTimeUtc;
        var ArrivalTimeUtc = userFlight.Flight?.ArrivalTimeUtc;

        if (DepartureTimeUtc.HasValue && ArrivalTimeUtc.HasValue)
        {
            var time = (ArrivalTimeUtc.Value - DepartureTimeUtc.Value).TotalMinutes;
            return (int)Math.Round(time, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static UserFlightDto MapToDto(UserFlight userFlight)
    {
        return new UserFlightDto
        {
            Id = userFlight.Id,
            UserId = userFlight.UserId,
            FlightId = userFlight.FlightId,
            FlightClass = userFlight.FlightClass,
            SeatNumber = userFlight.SeatNumber,
            BookedOnUtc = userFlight.BookedOnUtc,
            Notes = userFlight.Notes,
            DidFly = userFlight.DidFly,
            FlightNumber = userFlight.Flight?.FlightNumber ?? string.Empty,
            FlightStatus = userFlight.Flight?.Status ?? FlightStatus.Scheduled,
            DepartureTimeUtc = userFlight.Flight?.DepartureTimeUtc ?? DateTime.MinValue,
            ArrivalTimeUtc = userFlight.Flight?.ArrivalTimeUtc ?? DateTime.MinValue,
            DepartureAirportCode = userFlight.Flight?.DepartureAirport?.Code ?? string.Empty,
            DepartureAirportName = userFlight.Flight?.DepartureAirport?.Name ?? string.Empty,
            DepartureCity = userFlight.Flight?.DepartureAirport?.City ?? string.Empty,
            ArrivalAirportCode = userFlight.Flight?.ArrivalAirport?.Code ?? string.Empty,
            ArrivalAirportName = userFlight.Flight?.ArrivalAirport?.Name ?? string.Empty,
            ArrivalCity = userFlight.Flight?.ArrivalAirport?.City ?? string.Empty
        };
    }
}
