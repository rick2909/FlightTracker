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
    private readonly IAirportService _airportService;
    private readonly IFlightService _flightService;

    public UserFlightService(
        IUserFlightRepository userFlightRepository,
        IFlightRepository flightRepository,
        IAirportService airportService,
        IFlightService flightService)
    {
        _userFlightRepository = userFlightRepository;
        _flightRepository = flightRepository;
        _airportService = airportService;
        _flightService = flightService;
    }

    public async Task<IEnumerable<UserFlightDto>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsAsync(userId, cancellationToken);
        var list = new List<UserFlightDto>();
        foreach (var uf in userFlights)
        {
            list.Add(await MapToDtoAsync(uf, cancellationToken));
        }
        return list;
    }

    public async Task<UserFlightDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var userFlight = await _userFlightRepository.GetByIdAsync(id, cancellationToken);
    return userFlight != null ? await MapToDtoAsync(userFlight, cancellationToken) : null;
    }

    public async Task<IEnumerable<UserFlightDto>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default)
    {
        var userFlights = await _userFlightRepository.GetUserFlightsByClassAsync(userId, flightClass, cancellationToken);
        var list = new List<UserFlightDto>();
        foreach (var uf in userFlights)
        {
            list.Add(await MapToDtoAsync(uf, cancellationToken));
        }
        return list;
    }

    public async Task<UserFlightDto> AddUserFlightAsync(int userId, CreateUserFlightDto createDto, CancellationToken cancellationToken = default)
    {
        Flight? flight = null;
        // If FlightId provided, use it; otherwise attempt to create a Flight from provided fields
        if (createDto.FlightId > 0)
        {
            flight = await _flightRepository.GetByIdAsync(createDto.FlightId, cancellationToken);

            if (flight == null)
            {
                throw new ArgumentException($"Flight with ID {createDto.FlightId} not found.", nameof(createDto.FlightId));
            }
        }

        if (string.IsNullOrWhiteSpace(createDto.FlightNumber)
            || string.IsNullOrWhiteSpace(createDto.DepartureAirportCode)
            || string.IsNullOrWhiteSpace(createDto.ArrivalAirportCode)
            || !createDto.DepartureTimeUtc.HasValue
            || !createDto.ArrivalTimeUtc.HasValue)
        {
            throw new ArgumentException("Missing fields to create a new Flight. Provide FlightNumber, departure/arrival airport codes, and both times.");
        }

        var depAirport = await _airportService.GetAirportByCodeAsync(createDto.DepartureAirportCode!, cancellationToken);
        var arrAirport = await _airportService.GetAirportByCodeAsync(createDto.ArrivalAirportCode!, cancellationToken);
        if (depAirport is null || arrAirport is null)
        {
            throw new ArgumentException("Invalid airport code(s) provided.");
        }

        flight = new Flight
        {
            FlightNumber = createDto.FlightNumber!,
            Status = FlightStatus.Scheduled,
            DepartureAirportId = depAirport.Id,
            ArrivalAirportId = arrAirport.Id,
            DepartureTimeUtc = createDto.DepartureTimeUtc!.Value,
            ArrivalTimeUtc = createDto.ArrivalTimeUtc!.Value
        };

        flight = await _flightService.AddFlightAsync(flight, cancellationToken);

        // Check if user has already recorded this flight
        var hasFlown = await _userFlightRepository.HasUserFlownFlightAsync(userId, flight.Id, cancellationToken);
        if (hasFlown)
        {
            throw new InvalidOperationException($"User {userId} has already recorded flight {flight.Id}.");
        }

        var userFlight = new UserFlight
        {
            UserId = userId,
            FlightId = flight.Id,
            FlightClass = createDto.FlightClass,
            SeatNumber = createDto.SeatNumber,
            Notes = createDto.Notes,
            BookedOnUtc = DateTime.UtcNow
        };

        var savedUserFlight = await _userFlightRepository.AddAsync(userFlight, cancellationToken);
        
        // Reload to get navigation properties
        var reloadedUserFlight = await _userFlightRepository.GetByIdAsync(savedUserFlight.Id, cancellationToken);
    return await MapToDtoAsync(reloadedUserFlight!, cancellationToken);
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

        var updatedUserFlight = await _userFlightRepository.UpdateAsync(existingUserFlight, cancellationToken);
    return await MapToDtoAsync(updatedUserFlight, cancellationToken);
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

    private async Task<UserFlightDto> MapToDtoAsync(UserFlight userFlight, CancellationToken cancellationToken = default)
    {
        // get aircraft details from flight.
        var aircraft = userFlight.Flight?.Aircraft != null
            ? new AircraftDto
            {
                Id = userFlight.Flight.Aircraft.Id,
                Registration = userFlight.Flight.Aircraft.Registration,
                Manufacturer = userFlight.Flight.Aircraft.Manufacturer,
                Model = userFlight.Flight.Aircraft.Model,
                YearManufactured = userFlight.Flight.Aircraft.YearManufactured,
                PassengerCapacity = userFlight.Flight.Aircraft.PassengerCapacity,
                IcaoTypeCode = userFlight.Flight.Aircraft.IcaoTypeCode,
                Notes = userFlight.Flight.Aircraft.Notes,
                AirlineId = userFlight.Flight.Aircraft.AirlineId,
                AirlineIcaoCode = userFlight.Flight.Aircraft.Airline?.IcaoCode,
                AirlineIataCode = userFlight.Flight.Aircraft.Airline?.IataCode,
                AirlineName = userFlight.Flight.Aircraft.Airline?.Name
            }
            : null;

        // Resolve time zones via airport codes if available
        string? depTz = null;
        string? arrTz = null;
        if (!string.IsNullOrWhiteSpace(userFlight.Flight?.DepartureAirport?.IataCode) || !string.IsNullOrWhiteSpace(userFlight.Flight?.DepartureAirport?.IcaoCode))
        {
            var code = userFlight.Flight!.DepartureAirport!.IataCode ?? userFlight.Flight!.DepartureAirport!.IcaoCode!;
            depTz = await _airportService.GetTimeZoneIdByAirportCodeAsync(code, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(userFlight.Flight?.ArrivalAirport?.IataCode) || !string.IsNullOrWhiteSpace(userFlight.Flight?.ArrivalAirport?.IcaoCode))
        {
            var code = userFlight.Flight!.ArrivalAirport!.IataCode ?? userFlight.Flight!.ArrivalAirport!.IcaoCode!;
            arrTz = await _airportService.GetTimeZoneIdByAirportCodeAsync(code, cancellationToken);
        }

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
            OperatingAirlineId = userFlight.Flight?.OperatingAirlineId,
            OperatingAirlineIcaoCode = userFlight.Flight?.OperatingAirline?.IcaoCode,
            OperatingAirlineIataCode = userFlight.Flight?.OperatingAirline?.IataCode,
            OperatingAirlineName = userFlight.Flight?.OperatingAirline?.Name,
            DepartureAirportCode = userFlight.Flight?.DepartureAirport?.IataCode ?? userFlight.Flight?.DepartureAirport?.IcaoCode ?? string.Empty,
            DepartureIataCode = userFlight.Flight?.DepartureAirport?.IataCode,
            DepartureIcaoCode = userFlight.Flight?.DepartureAirport?.IcaoCode,
            DepartureAirportName = userFlight.Flight?.DepartureAirport?.Name ?? string.Empty,
            DepartureCity = userFlight.Flight?.DepartureAirport?.City ?? string.Empty,
            ArrivalAirportCode = userFlight.Flight?.ArrivalAirport?.IataCode ?? userFlight.Flight?.ArrivalAirport?.IcaoCode ?? string.Empty,
            ArrivalIataCode = userFlight.Flight?.ArrivalAirport?.IataCode,
            ArrivalIcaoCode = userFlight.Flight?.ArrivalAirport?.IcaoCode,
            ArrivalAirportName = userFlight.Flight?.ArrivalAirport?.Name ?? string.Empty,
            ArrivalCity = userFlight.Flight?.ArrivalAirport?.City ?? string.Empty,
            DepartureTimeZoneId = depTz,
            ArrivalTimeZoneId = arrTz,
            Aircraft = aircraft
        };
    }
}
