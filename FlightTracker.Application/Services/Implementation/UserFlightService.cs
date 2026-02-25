using AutoMapper;
using System.Net.Http;
using FlightTracker.Application.Dtos;
using FlightTracker.Application.Dtos.Validation;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using FlightTracker.Application.Repositories.Interfaces;
using FluentValidation;
using System.Diagnostics;

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
    private readonly IFlightMetadataProvisionService _metadataProvisionService;
    private readonly IAirlineRepository _airlineRepository;
    private readonly IAircraftRepository _aircraftRepository;
    private readonly IAircraftLookupClient _aircraftLookupClient;
    private readonly IAirportEnrichmentService _airportEnrichmentService;
    private readonly IMapper _mapper;
    private readonly IClock _clock;

    public UserFlightService(
        IUserFlightRepository userFlightRepository,
        IFlightRepository flightRepository,
        IAirportService airportService,
        IFlightService flightService,
        IFlightMetadataProvisionService metadataProvisionService,
        IAirlineRepository airlineRepository,
        IAircraftRepository aircraftRepository,
        IAircraftLookupClient aircraftLookupClient,
        IAirportEnrichmentService airportEnrichmentService,
        IMapper mapper,
        IClock clock)
    {
        _userFlightRepository = userFlightRepository;
        _flightRepository = flightRepository;
        _airportService = airportService;
        _flightService = flightService;
        _metadataProvisionService = metadataProvisionService;
        _airlineRepository = airlineRepository;
        _aircraftRepository = aircraftRepository;
        _aircraftLookupClient = aircraftLookupClient;
        _airportEnrichmentService = airportEnrichmentService;
        _mapper = mapper;
        _clock = clock;
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
            BookedOnUtc = _clock.UtcNow
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
        // Resolve time zones via airport codes if available
        var depCode = userFlight.Flight?.DepartureAirport?.IataCode ?? userFlight.Flight?.DepartureAirport?.IcaoCode;
        var arrCode = userFlight.Flight?.ArrivalAirport?.IataCode ?? userFlight.Flight?.ArrivalAirport?.IcaoCode;
        string? depTz = depCode is { Length: > 0 }
            ? (await _airportService
                .GetTimeZoneIdByAirportCodeAsync(depCode, cancellationToken))
                .Value
            : null;
        string? arrTz = arrCode is { Length: > 0 }
            ? (await _airportService
                .GetTimeZoneIdByAirportCodeAsync(arrCode, cancellationToken))
                .Value
            : null;

        return _mapper.Map<UserFlightDto>(userFlight, opt =>
        {
            opt.Items["DepartureTimeZoneId"] = depTz;
            opt.Items["ArrivalTimeZoneId"] = arrTz;
        });
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

        // Provision airports and airline (idempotent) using callsign/flight number
        try
        {
            await _metadataProvisionService.EnsureAirlineAndAirportsForCallsignAsync(dto.FlightNumber!, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Non-fatal; continue with local lookup
            Debug.WriteLine("External API call failed during flight creation. Proceeding with local data only.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Non-fatal timeout; continue with local lookup
            Debug.WriteLine("External API call timed out during flight creation. Proceeding with local data only.");
        }

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

        // Resolve operating airline from code or flight number prefix
        var airlineCode = dto.OperatingAirlineCode;
        if (string.IsNullOrWhiteSpace(airlineCode))
        {
            // Try to extract from flight number
            var prefix = new string(dto.FlightNumber!.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                airlineCode = prefix;
            }
        }

        if (!string.IsNullOrWhiteSpace(airlineCode))
        {
            var al = await _airlineRepository.GetByIataAsync(airlineCode, cancellationToken)
                     ?? await _airlineRepository.GetByIcaoAsync(airlineCode, cancellationToken);
            if (al is not null)
            {
                flight.OperatingAirlineId = al.Id;
            }
        }

        // Handle aircraft registration
        if (!string.IsNullOrWhiteSpace(dto.AircraftRegistration))
        {
            var aircraft = await ResolveOrCreateAircraftAsync(
                dto.AircraftRegistration,
                flight.OperatingAirlineId,
                cancellationToken);

            if (aircraft is not null)
            {
                flight.AircraftId = aircraft.Id;
            }
        }

        var addFlightResult = await _flightService.AddFlightAsync(
            flight,
            cancellationToken);

        if (addFlightResult.IsFailure || addFlightResult.Value is null)
        {
            throw new InvalidOperationException(
                addFlightResult.ErrorMessage
                ?? "Failed to create flight.");
        }

        return addFlightResult.Value;
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

        // Resolve operating airline from code or flight number prefix
        var airlineCode = schedule.OperatingAirlineCode;
        if (string.IsNullOrWhiteSpace(airlineCode))
        {
            var prefix = new string(schedule.FlightNumber.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                airlineCode = prefix;
            }
        }

        if (!string.IsNullOrWhiteSpace(airlineCode))
        {
            var al = await _airlineRepository.GetByIataAsync(airlineCode, cancellationToken)
                     ?? await _airlineRepository.GetByIcaoAsync(airlineCode, cancellationToken);
            flight.OperatingAirlineId = al?.Id;
        }

        // Handle aircraft registration
        if (!string.IsNullOrWhiteSpace(schedule.AircraftRegistration))
        {
            var aircraft = await ResolveOrCreateAircraftAsync(
                schedule.AircraftRegistration,
                flight.OperatingAirlineId,
                cancellationToken);

            flight.AircraftId = aircraft?.Id;
        }
        else
        {
            // Clear aircraft if registration is empty
            flight.AircraftId = null;
        }

        var updateFlightResult = await _flightService.UpdateFlightAsync(
            flight,
            cancellationToken);

        if (updateFlightResult.IsFailure)
        {
            throw new InvalidOperationException(
                updateFlightResult.ErrorMessage
                ?? "Failed to update flight.");
        }
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
        var airportResult = await _airportService.GetAirportByCodeAsync(
            code,
            cancellationToken);

        if (airportResult.IsFailure)
        {
            throw new InvalidOperationException(
                airportResult.ErrorMessage
                ?? "Failed to resolve airport.");
        }

        var airport = airportResult.Value;

        if (airport is null)
        {
            // Try to enrich airport from external API if not found
            try
            {
                var enrichedAirport = await _airportEnrichmentService.EnrichAirportAsync(code, cancellationToken);
                if (enrichedAirport is not null)
                {
                    return enrichedAirport.Id;
                }
            }
            catch (HttpRequestException)
            {
                // Non-fatal; enrichment failed, continue to throw below
                Debug.WriteLine($"Failed to enrich airport {code} from AirportDB API.");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"Timeout enriching airport {code} from AirportDB API.");
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"{ex.Message}. Get ICAO code from https://www.flightradar24.com/data/airport/{code} or https://airport-data.com/world-airports/{code}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error enriching airport {code}: {ex.Message}");
            }

            throw new ArgumentException("Invalid airport code(s) provided.");
        }
        return airport.Id;
    }

    /// <summary>
    /// Resolves an aircraft by registration. If not found, creates a minimal aircraft record.
    /// </summary>
    private async Task<Aircraft?> ResolveOrCreateAircraftAsync(
        string registration,
        int? airlineId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(registration))
        {
            return null;
        }

        var normalized = NormalizeRegistration(registration);
        if (normalized is null)
        {
            return null;
        }

        // Check if aircraft already exists
        var existing = await _aircraftRepository.GetByRegistrationAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        // Try to fetch from AdsBdb to enrich data
        AircraftEnrichmentDto? adsbData = null;
        try
        {
            adsbData = await _aircraftLookupClient.GetAircraftAsync(normalized, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Non-fatal; continue with minimal data
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Non-fatal timeout; continue with minimal data
        }

        // Create new aircraft with data from AdsBdb or defaults
        var model = TruncateOrDefault(adsbData?.Type, 64, "Unknown");
        var icaoType = TruncateOrNull(adsbData?.IcaoType, 4)?.ToUpperInvariant();
        var modeS = TruncateOrNull(adsbData?.ModeS, 6)?.ToUpperInvariant();
        var notes = adsbData is not null
            ? TruncateOrNull($"Owner: {adsbData.RegisteredOwner}, Country: {adsbData.RegisteredOwnerCountry}", 500)
            : null;

        var newAircraft = new Aircraft
        {
            Registration = normalized,
            Manufacturer = ParseManufacturer(adsbData?.Manufacturer),
            Model = model,
            IcaoTypeCode = icaoType,
            ModeS = modeS,
            Notes = notes,
            AirlineId = airlineId
        };

        return await _aircraftRepository.AddAsync(newAircraft, cancellationToken);
    }

    /// <summary>
    /// Parses manufacturer name from string to enum. Returns Other if not recognized.
    /// </summary>
    private static AircraftManufacturer ParseManufacturer(string? manufacturerName)
    {
        if (string.IsNullOrWhiteSpace(manufacturerName))
            return AircraftManufacturer.Other;

        var upper = manufacturerName.ToUpperInvariant();

        return upper switch
        {
            _ when upper.Contains("BOEING") => AircraftManufacturer.Boeing,
            _ when upper.Contains("AIRBUS") => AircraftManufacturer.Airbus,
            _ when upper.Contains("EMBRAER") => AircraftManufacturer.Embraer,
            _ when upper.Contains("BOMBARDIER") => AircraftManufacturer.Bombardier,
            _ when upper.Contains("ATR") => AircraftManufacturer.ATR,
            _ when upper.Contains("CESSNA") => AircraftManufacturer.Cessna,
            _ when upper.Contains("GULFSTREAM") => AircraftManufacturer.Gulfstream,
            _ when upper.Contains("DASSAULT") => AircraftManufacturer.Dassault,
            _ when upper.Contains("LOCKHEED") => AircraftManufacturer.Lockheed,
            _ when upper.Contains("MCDONNELL") => AircraftManufacturer.McDonnellDouglas,
            _ when upper.Contains("SAAB") => AircraftManufacturer.Saab,
            _ when upper.Contains("TUPOLEV") => AircraftManufacturer.Tupolev,
            _ when upper.Contains("SUKHOI") => AircraftManufacturer.Sukhoi,
            _ when upper.Contains("COMAC") => AircraftManufacturer.COMAC,
            _ when upper.Contains("MITSUBISHI") => AircraftManufacturer.Mitsubishi,
            _ => AircraftManufacturer.Other
        };
    }

    private static string? NormalizeRegistration(string registration)
    {
        var normalized = registration.Trim().ToUpperInvariant();
        return normalized.Length > 8 ? null : normalized;
    }

    private static string? TruncateOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength
            ? trimmed.Substring(0, maxLength)
            : trimmed;
    }

    private static string TruncateOrDefault(string? value, int maxLength, string fallback)
    {
        var trimmed = TruncateOrNull(value, maxLength);
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }
}
