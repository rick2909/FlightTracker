using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
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

		if (!context.Aircraft.Any())
		{
			var aircraft = GetSeedAircraft();
			await context.Aircraft.AddRangeAsync(aircraft, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
		}

		if (!context.Flights.Any())
		{
			var airports = context.Airports.Local.Any() ? context.Airports.Local.ToList() : await context.Airports.ToListAsync(cancellationToken);
			var aircraft = context.Aircraft.Local.Any() ? context.Aircraft.Local.ToList() : await context.Aircraft.ToListAsync(cancellationToken);
			var flights = GetSeedFlights(airports, aircraft);
			await context.Flights.AddRangeAsync(flights, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
		}

		if (userManager != null)
		{
			await SeedUsersAsync(userManager, cancellationToken);
			
			// Seed UserFlights after users are created
			if (!context.UserFlights.Any())
			{
				await SeedUserFlightsAsync(context, userManager, cancellationToken);
			}
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

	private static IReadOnlyList<Aircraft> GetSeedAircraft() => new List<Aircraft>
	{
		new() { Registration = "N12345", Manufacturer = AircraftManufacturer.Boeing, Model = "737-800", YearManufactured = 2018, PassengerCapacity = 189, IcaoTypeCode = "B738" },
		new() { Registration = "N67890", Manufacturer = AircraftManufacturer.Airbus, Model = "A320", YearManufactured = 2020, PassengerCapacity = 180, IcaoTypeCode = "A320" },
		new() { Registration = "G-ABCD", Manufacturer = AircraftManufacturer.Boeing, Model = "777-200", YearManufactured = 2019, PassengerCapacity = 314, IcaoTypeCode = "B772" },
		new() { Registration = "D-EFGH", Manufacturer = AircraftManufacturer.Airbus, Model = "A350-900", YearManufactured = 2021, PassengerCapacity = 325, IcaoTypeCode = "A359" },
		new() { Registration = "JA1234", Manufacturer = AircraftManufacturer.Boeing, Model = "787-9", YearManufactured = 2022, PassengerCapacity = 290, IcaoTypeCode = "B789" }
	};

	private static IReadOnlyList<Flight> GetSeedFlights(IReadOnlyList<Airport> airports, IReadOnlyList<Aircraft> aircraft)
	{
		// Deterministic base time for tests (UTC)
		var baseTime = new DateTime(2025, 01, 01, 08, 00, 00, DateTimeKind.Utc);
		Airport A(string code) => airports.First(a => a.Code == code);
		Aircraft Ac(string registration) => aircraft.First(a => a.Registration == registration);

		return new List<Flight>
		{
			new()
			{
				FlightNumber = "FT100", Status = FlightStatus.Scheduled,
				DepartureAirportId = A("JFK").Id, ArrivalAirportId = A("LAX").Id,
				AircraftId = Ac("N12345").Id,
				DepartureTimeUtc = baseTime.AddHours(2), ArrivalTimeUtc = baseTime.AddHours(8)
			},
			new()
			{
				FlightNumber = "FT101", Status = FlightStatus.Scheduled,
				DepartureAirportId = A("LAX").Id, ArrivalAirportId = A("JFK").Id,
				AircraftId = Ac("N67890").Id,
				DepartureTimeUtc = baseTime.AddHours(3), ArrivalTimeUtc = baseTime.AddHours(9)
			},
			new()
			{
				FlightNumber = "FT200", Status = FlightStatus.Delayed,
				DepartureAirportId = A("LHR").Id, ArrivalAirportId = A("FRA").Id,
				AircraftId = Ac("G-ABCD").Id,
				DepartureTimeUtc = baseTime.AddHours(1), ArrivalTimeUtc = baseTime.AddHours(3)
			},
			new()
			{
				FlightNumber = "FT300", Status = FlightStatus.Scheduled,
				DepartureAirportId = A("FRA").Id, ArrivalAirportId = A("NRT").Id,
				AircraftId = Ac("D-EFGH").Id,
				DepartureTimeUtc = baseTime.AddHours(5), ArrivalTimeUtc = baseTime.AddHours(17)
			},
			new()
			{
				FlightNumber = "FT400", Status = FlightStatus.Cancelled,
				DepartureAirportId = A("NRT").Id, ArrivalAirportId = A("LHR").Id,
				AircraftId = Ac("JA1234").Id,
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

	private static async Task SeedUserFlightsAsync(FlightTrackerDbContext context, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
	{
		var demoUser = await userManager.FindByNameAsync("demo");
		var adminUser = await userManager.FindByNameAsync("admin");
		
		if (demoUser == null || adminUser == null) return;

		var flights = await context.Flights.ToListAsync(cancellationToken);
		if (!flights.Any()) return;

		var baseTime = DateTime.UtcNow;
		
		var userFlights = new List<UserFlight>
		{
			// Demo user's flights
			new()
			{
				UserId = demoUser.Id,
				FlightId = flights.First(f => f.FlightNumber == "FT100").Id,
				FlightClass = FlightClass.Economy,
				SeatNumber = "12A",
				BookedOnUtc = baseTime.AddDays(-30),
				Notes = "Nice window seat, good service",
				DidFly = true
			},
			new()
			{
				UserId = demoUser.Id,
				FlightId = flights.First(f => f.FlightNumber == "FT200").Id,
				FlightClass = FlightClass.Business,
				SeatNumber = "3B",
				BookedOnUtc = baseTime.AddDays(-15),
				Notes = "Delayed but comfortable business class",
				DidFly = true
			},
			// Admin user's flights
			new()
			{
				UserId = adminUser.Id,
				FlightId = flights.First(f => f.FlightNumber == "FT101").Id,
				FlightClass = FlightClass.PremiumEconomy,
				SeatNumber = "8C",
				BookedOnUtc = baseTime.AddDays(-20),
				Notes = "Extra legroom was worth it",
				DidFly = true
			},
			new()
			{
				UserId = adminUser.Id,
				FlightId = flights.First(f => f.FlightNumber == "FT300").Id,
				FlightClass = FlightClass.First,
				SeatNumber = "1A",
				BookedOnUtc = baseTime.AddDays(-10),
				Notes = "Amazing first class experience on long haul",
				DidFly = true
			}
		};

		await context.UserFlights.AddRangeAsync(userFlights, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
	}
}