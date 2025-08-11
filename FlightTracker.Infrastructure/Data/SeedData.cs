using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Data;

/// <summary>
/// Provides deterministic seed data for development and tests.
/// Call <see cref="EnsureSeededAsync"/> at startup or inside integration tests.
/// </summary>
public static class SeedData
{
	/// <summary>
	/// Ensures the database is created and seeded (idempotent). Suitable for app startup.
	/// </summary>
	public static async Task EnsureSeededAsync(FlightTrackerDbContext context, UserManager<ApplicationUser>? userManager = null, CancellationToken cancellationToken = default)
	{
		await context.Database.EnsureCreatedAsync(cancellationToken);
		if (await context.Airports.AnyAsync(cancellationToken))
			return; // Already seeded

		await SeedAsync(context, userManager, cancellationToken);
	}

	/// <summary>
	/// Seeds airports, flights and optionally users. Use in tests with an InMemory database.
	/// Will not duplicate if run multiple times in the same context instance.
	/// </summary>
	public static async Task SeedAsync(FlightTrackerDbContext context, UserManager<ApplicationUser>? userManager = null, CancellationToken cancellationToken = default)
	{
		if (!context.Airports.Any())
		{
			var airports = GetSeedAirports();
			await context.Airports.AddRangeAsync(airports, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
		}

		if (!context.Flights.Any())
		{
			var flights = GetSeedFlights(context.Airports.Local.Any() ? context.Airports.Local.ToList() : await context.Airports.ToListAsync(cancellationToken));
			await context.Flights.AddRangeAsync(flights, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
		}

		if (userManager != null)
		{
			await SeedUsersAsync(userManager, cancellationToken);
		}
	}

	private static IReadOnlyList<Airport> GetSeedAirports() => new List<Airport>
	{
		new() { Code = "JFK", Name = "John F. Kennedy International", City = "New York", Country = "USA" },
		new() { Code = "LAX", Name = "Los Angeles International", City = "Los Angeles", Country = "USA" },
		new() { Code = "LHR", Name = "Heathrow", City = "London", Country = "UK" },
		new() { Code = "FRA", Name = "Frankfurt am Main", City = "Frankfurt", Country = "Germany" },
		new() { Code = "NRT", Name = "Narita International", City = "Tokyo", Country = "Japan" }
	};

	private static IReadOnlyList<Flight> GetSeedFlights(IReadOnlyList<Airport> airports)
	{
		// Deterministic base time for tests (UTC)
		var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
		Airport A(string code) => airports.First(a => a.Code == code);

		return new List<Flight>
		{
			new()
			{
				FlightNumber = "FT100", Status = "Scheduled",
				DepartureAirportId = A("JFK").Id, ArrivalAirportId = A("LAX").Id,
				DepartureTimeUtc = baseTime.AddHours(2), ArrivalTimeUtc = baseTime.AddHours(8)
			},
			new()
			{
				FlightNumber = "FT101", Status = "Scheduled",
				DepartureAirportId = A("LAX").Id, ArrivalAirportId = A("JFK").Id,
				DepartureTimeUtc = baseTime.AddHours(3), ArrivalTimeUtc = baseTime.AddHours(9)
			},
			new()
			{
				FlightNumber = "FT200", Status = "Delayed",
				DepartureAirportId = A("LHR").Id, ArrivalAirportId = A("FRA").Id,
				DepartureTimeUtc = baseTime.AddHours(1), ArrivalTimeUtc = baseTime.AddHours(3)
			},
			new()
			{
				FlightNumber = "FT300", Status = "Scheduled",
				DepartureAirportId = A("FRA").Id, ArrivalAirportId = A("NRT").Id,
				DepartureTimeUtc = baseTime.AddHours(5), ArrivalTimeUtc = baseTime.AddHours(17)
			},
			new()
			{
				FlightNumber = "FT400", Status = "Cancelled",
				DepartureAirportId = A("NRT").Id, ArrivalAirportId = A("LHR").Id,
				DepartureTimeUtc = baseTime.AddHours(6), ArrivalTimeUtc = baseTime.AddHours(18)
			}
		};
	}

	private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, CancellationToken ct)
	{
		async Task EnsureUserAsync(string userName, string email, string password)
		{
			var existing = await userManager.FindByNameAsync(userName);
			if (existing != null) return;
			var user = new ApplicationUser
			{
				UserName = userName,
				Email = email,
				EmailConfirmed = true
			};
			await userManager.CreateAsync(user, password);
		}

		await EnsureUserAsync("admin", "admin@example.com", "Admin#123");
		await EnsureUserAsync("demo", "demo@example.com", "Demo#123");
	}
}