using FlightTracker.Application.Dtos;
using FlightTracker.Application.Dtos.Validation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using FlightTracker.Application.Repositories.Interfaces;
using FluentValidation;

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
    // Validate input
    new CreateUserFlightDtoValidator().ValidateAndThrow(createDto);
                // Use existing flight if provided; otherwise create from fields
                Flight flight = createDto.FlightId > 0
                        ? await _flightRepository.GetByIdAsync(createDto.FlightId, cancellationToken)
                            ?? throw new ArgumentException($"Flight with ID {createDto.FlightId} not found.", nameof(createDto.FlightId))
                        : await CreateFlightFromDtoAsync(createDto, cancellationToken);

        // Check duplicate user-flight record
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
            DidFly = createDto.DidFly,
            BookedOnUtc = DateTime.UtcNow
        };

        var savedUserFlight = await _userFlightRepository.AddAsync(userFlight, cancellationToken);
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
        existingUserFlight.DidFly = updateDto.DidFly;

        var updatedUserFlight = await _userFlightRepository.UpdateAsync(existingUserFlight, cancellationToken);
        return await MapToDtoAsync(updatedUserFlight, cancellationToken);
    }

    public async Task<UserFlightDto?> UpdateUserFlightAndScheduleAsync(
        int id,
        UpdateUserFlightDto userFlight,
        FlightScheduleUpdateDto schedule,
        CancellationToken cancellationToken = default)
    {
    // Validate inputs
    new UpdateUserFlightDtoValidator().ValidateAndThrow(userFlight);
    new FlightScheduleUpdateDtoValidator().ValidateAndThrow(schedule);
    var existingUserFlight = await _userFlightRepository.GetByIdAsync(id, cancellationToken);
    if (existingUserFlight == null) return null;

    var flight = await _flightRepository.GetByIdAsync(schedule.FlightId, cancellationToken);
    if (flight is null) return null;

    await UpdateFlightScheduleAsync(flight, schedule, cancellationToken);

    // Update user flight fields
    existingUserFlight.FlightClass = userFlight.FlightClass;
    existingUserFlight.SeatNumber = userFlight.SeatNumber;
    existingUserFlight.Notes = userFlight.Notes;
    existingUserFlight.DidFly = userFlight.DidFly;
    var updatedUserFlight = await _userFlightRepository.UpdateAsync(existingUserFlight, cancellationToken);

    // Reload with navs
    var reloaded = await _userFlightRepository.GetByIdAsync(updatedUserFlight.Id, cancellationToken);
    return await MapToDtoAsync(reloaded!, cancellationToken);
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
        var departureTimeUtc = userFlight.Flight?.DepartureTimeUtc;
        var arrivalTimeUtc = userFlight.Flight?.ArrivalTimeUtc;

        if (departureTimeUtc.HasValue && arrivalTimeUtc.HasValue)
        {
            var time = (arrivalTimeUtc.Value - departureTimeUtc.Value).TotalMinutes;
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
                IcaoTypeCode = userFlight.Flight.Aircraft.IcaoTypeCode,
                Notes = userFlight.Flight.Aircraft.Notes
            }
            : null;

        // Resolve time zones via airport codes if available
        var depCode = userFlight.Flight?.DepartureAirport?.IataCode ?? userFlight.Flight?.DepartureAirport?.IcaoCode;
        var arrCode = userFlight.Flight?.ArrivalAirport?.IataCode ?? userFlight.Flight?.ArrivalAirport?.IcaoCode;
        string? depTz = depCode is { Length: > 0 }
            ? await _airportService.GetTimeZoneIdByAirportCodeAsync(depCode, cancellationToken)
            : null;
        string? arrTz = arrCode is { Length: > 0 }
            ? await _airportService.GetTimeZoneIdByAirportCodeAsync(arrCode, cancellationToken)
            : null;

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

    private async Task<Flight> CreateFlightFromDtoAsync(CreateUserFlightDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.FlightNumber)
            || string.IsNullOrWhiteSpace(dto.DepartureAirportCode)
            || string.IsNullOrWhiteSpace(dto.ArrivalAirportCode)
            || !dto.DepartureTimeUtc.HasValue
            || !dto.ArrivalTimeUtc.HasValue)
        {
            throw new ArgumentException("Missing fields to create a new Flight. Provide FlightNumber, departure/arrival airport codes, and both times.");
        }

        EnsureArrivalAfterDeparture(dto.DepartureTimeUtc!.Value, dto.ArrivalTimeUtc!.Value);

        var depId = await ResolveAirportIdOrThrowAsync(dto.DepartureAirportCode!, cancellationToken);
        var arrId = await ResolveAirportIdOrThrowAsync(dto.ArrivalAirportCode!, cancellationToken);

        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber!,
            Status = FlightStatus.Scheduled,
            DepartureAirportId = depId,
            ArrivalAirportId = arrId,
            DepartureTimeUtc = dto.DepartureTimeUtc!.Value,
            ArrivalTimeUtc = dto.ArrivalTimeUtc!.Value
        };

        return await _flightService.AddFlightAsync(flight, cancellationToken);
    }

    private async Task UpdateFlightScheduleAsync(Flight flight, FlightScheduleUpdateDto schedule, CancellationToken cancellationToken)
    {
        EnsureArrivalAfterDeparture(schedule.DepartureTimeUtc, schedule.ArrivalTimeUtc);

        var depId = await ResolveAirportIdOrThrowAsync(schedule.DepartureAirportCode, cancellationToken);
        var arrId = await ResolveAirportIdOrThrowAsync(schedule.ArrivalAirportCode, cancellationToken);

        flight.FlightNumber = schedule.FlightNumber;
        flight.DepartureAirportId = depId;
        flight.ArrivalAirportId = arrId;
        flight.DepartureTimeUtc = schedule.DepartureTimeUtc;
        flight.ArrivalTimeUtc = schedule.ArrivalTimeUtc;
        await _flightService.UpdateFlightAsync(flight, cancellationToken);
    }

    private static void EnsureArrivalAfterDeparture(DateTime departureUtc, DateTime arrivalUtc)
    {
        if (arrivalUtc <= departureUtc)
        {
            throw new ArgumentException("Arrival time must be after departure time.");
        }
    }

    private async Task<int> ResolveAirportIdOrThrowAsync(string code, CancellationToken cancellationToken)
    {
        var airport = await _airportService.GetAirportByCodeAsync(code, cancellationToken);
        if (airport is null)
        {
            throw new ArgumentException("Invalid airport code(s) provided.");
        }
        return airport.Id;
    }
}
