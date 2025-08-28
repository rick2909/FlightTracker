using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Application.Repositories.Interfaces;
using FlightTracker.Application.Services.Interfaces;
using FlightTracker.Domain.Entities;

namespace FlightTracker.Application.Services.Implementation;

public sealed class FlightMetadataProvisionService : IFlightMetadataProvisionService
{
    private readonly IFlightRouteLookupClient _lookupClient;
    private readonly IAirlineRepository _airlineRepository;
    private readonly IAirportRepository _airportRepository;

    public FlightMetadataProvisionService(
        IFlightRouteLookupClient lookupClient,
        IAirlineRepository airlineRepository,
        IAirportRepository airportRepository)
    {
        _lookupClient = lookupClient;
        _airlineRepository = airlineRepository;
        _airportRepository = airportRepository;
    }

    public async Task EnsureAirlineAndAirportsForCallsignAsync(string callsign, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(callsign)) return;

        var route = await _lookupClient.GetFlightRouteAsync(callsign.Trim(), cancellationToken);
        if (route is null) return;

        // Airline
        if (route.Airline is { } al)
        {
            Airline? existing = null;
            if (!string.IsNullOrWhiteSpace(al.Iata))
            {
                existing = await _airlineRepository.GetByIataAsync(al.Iata!, cancellationToken);
            }
            if (existing is null && !string.IsNullOrWhiteSpace(al.Icao))
            {
                existing = await _airlineRepository.GetByIcaoAsync(al.Icao!, cancellationToken);
            }
            if (existing is null)
            {
                var newAirline = new Airline
                {
                    Name = al.Name ?? al.Icao ?? al.Iata ?? "Unknown Airline",
                    IcaoCode = al.Icao ?? string.Empty,
                    IataCode = al.Iata,
                    Country = al.CountryName ?? string.Empty,
                    Active = true
                };
                await _airlineRepository.AddAsync(newAirline, cancellationToken);
            }
        }

        // Airports
        async Task EnsureAirportAsync(FlightRouteAirport? a)
        {
            if (a is null) return;
            var byCode = a.IataCode ?? a.IcaoCode;
            Airport? existing = null;
            if (!string.IsNullOrWhiteSpace(byCode))
            {
                existing = await _airportRepository.GetByCodeAsync(byCode!, cancellationToken);
            }
            if (existing is null && !string.IsNullOrWhiteSpace(a.IcaoCode))
            {
                existing = await _airportRepository.GetByCodeAsync(a.IcaoCode!, cancellationToken);
            }
            if (existing is null && !string.IsNullOrWhiteSpace(a.IataCode))
            {
                existing = await _airportRepository.GetByCodeAsync(a.IataCode!, cancellationToken);
            }
            if (existing is null)
            {
                var city = a.Municipality ?? string.Empty;
                var country = a.CountryName ?? string.Empty;
                var name = a.Name ?? ($"{city} {country}".Trim());
                var newAirport = new Airport
                {
                    IataCode = a.IataCode?.ToUpperInvariant(),
                    IcaoCode = a.IcaoCode?.ToUpperInvariant(),
                    Name = string.IsNullOrWhiteSpace(name) ? (a.IataCode ?? a.IcaoCode ?? "Unknown Airport") : name,
                    City = city,
                    Country = country,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude
                };
                await _airportRepository.AddAsync(newAirport, cancellationToken);
            }
        }

        await EnsureAirportAsync(route.Origin);
        await EnsureAirportAsync(route.Destination);
    }
}
