using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;
using FlightTracker.Infrastructure.Data;
using FlightTracker.Application.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Repositories.Implementation;

public class FlightRepository(FlightTrackerDbContext db) : IFlightRepository
{
    public async Task<Flight?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<Flight?> GetByFlightNumberAndDateAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber
                && f.DepartureTimeUtc >= start && f.DepartureTimeUtc <= end, cancellationToken);
    }

    public async Task<IReadOnlyList<Flight>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Flight>> SearchByFlightNumberAsync(string flightNumber, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var normalized = (flightNumber ?? string.Empty).Trim().ToUpperInvariant();
        var query = db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .Where(f => f.FlightNumber == normalized);

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(f => f.DepartureTimeUtc >= start && f.DepartureTimeUtc <= end);
        }

        return await query
            .OrderBy(f => f.DepartureTimeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flight>> SearchByRouteAsync(string? departure, string? arrival, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var dep = departure?.Trim();
        var arr = arrival?.Trim();

        var query = db.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .AsNoTracking()
            .AsQueryable();

    if (!string.IsNullOrWhiteSpace(dep))
        {
            var depUpper = dep.ToUpperInvariant();
            query = query.Where(f => f.DepartureAirport != null && (
    (f.DepartureAirport.IataCode != null && f.DepartureAirport.IataCode.ToUpper() == depUpper) ||
    (f.DepartureAirport.IcaoCode != null && f.DepartureAirport.IcaoCode.ToUpper() == depUpper) ||
        (f.DepartureAirport.IataCode != null && f.DepartureAirport.IataCode.ToUpper() == depUpper) ||
        (f.DepartureAirport.IcaoCode != null && f.DepartureAirport.IcaoCode.ToUpper() == depUpper) ||
                EF.Functions.Like(f.DepartureAirport.City, $"%{dep}%") ||
                EF.Functions.Like(f.DepartureAirport.Name, $"%{dep}%")
            ));
        }

    if (!string.IsNullOrWhiteSpace(arr))
        {
            var arrUpper = arr.ToUpperInvariant();
            query = query.Where(f => f.ArrivalAirport != null && (
    (f.ArrivalAirport.IataCode != null && f.ArrivalAirport.IataCode.ToUpper() == arrUpper) ||
    (f.ArrivalAirport.IcaoCode != null && f.ArrivalAirport.IcaoCode.ToUpper() == arrUpper) ||
        (f.ArrivalAirport.IataCode != null && f.ArrivalAirport.IataCode.ToUpper() == arrUpper) ||
        (f.ArrivalAirport.IcaoCode != null && f.ArrivalAirport.IcaoCode.ToUpper() == arrUpper) ||
                EF.Functions.Like(f.ArrivalAirport.City, $"%{arr}%") ||
                EF.Functions.Like(f.ArrivalAirport.Name, $"%{arr}%")
            ));
        }

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(f => f.DepartureTimeUtc >= start && f.DepartureTimeUtc <= end);
        }

        return await query
            .OrderBy(f => f.DepartureTimeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Flight> AddAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        await db.Flights.AddAsync(flight, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return flight;
    }

    public async Task UpdateAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        db.Flights.Update(flight);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Flights.FindAsync(new object?[] { id }, cancellationToken);
        if (entity == null) return;
        db.Flights.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
