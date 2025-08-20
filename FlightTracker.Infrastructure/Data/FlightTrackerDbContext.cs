using FlightTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlightTracker.Infrastructure.Data;

/// <summary>
/// Primary EF Core DbContext including Identity and application entities.
/// </summary>
public class FlightTrackerDbContext(DbContextOptions<FlightTrackerDbContext> options)
	: IdentityDbContext<ApplicationUser, IdentityRole<int>, int>(options)
{
	public DbSet<Flight> Flights => Set<Flight>();
	public DbSet<Airport> Airports => Set<Airport>();
	public DbSet<UserFlight> UserFlights => Set<UserFlight>();
	public DbSet<Aircraft> Aircraft => Set<Aircraft>();
	public DbSet<Airline> Airlines => Set<Airline>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		// Configure Flight entity
		builder.Entity<Flight>(entity =>
		{
			entity.Property(f => f.FlightNumber).HasMaxLength(16).IsRequired();
			entity.Property(f => f.Status).HasConversion<string>().IsRequired();
			entity.Property(f => f.DepartureTerminal).HasMaxLength(8);
			entity.Property(f => f.DepartureGate).HasMaxLength(8);
			entity.Property(f => f.ArrivalTerminal).HasMaxLength(8);
			entity.Property(f => f.ArrivalGate).HasMaxLength(8);

			entity.HasOne(f => f.DepartureAirport)
				  .WithMany(a => a.DepartingFlights)
				  .HasForeignKey(f => f.DepartureAirportId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(f => f.ArrivalAirport)
				  .WithMany(a => a.ArrivingFlights)
				  .HasForeignKey(f => f.ArrivalAirportId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(f => f.Aircraft)
				  .WithMany(a => a.Flights)
				  .HasForeignKey(f => f.AircraftId)
				  .OnDelete(DeleteBehavior.SetNull);

			entity.HasOne(f => f.OperatingAirline)
				  .WithMany(al => al.Flights)
				  .HasForeignKey(f => f.OperatingAirlineId)
				  .OnDelete(DeleteBehavior.SetNull);

			entity.HasIndex(f => f.FlightNumber);
		});

		// Configure Aircraft entity
		builder.Entity<Aircraft>(entity =>
		{
			entity.Property(a => a.Registration).HasMaxLength(8).IsRequired();
			entity.Property(a => a.Manufacturer).HasConversion<string>().IsRequired();
			entity.Property(a => a.Model).HasMaxLength(64).IsRequired();
			entity.Property(a => a.IcaoTypeCode).HasMaxLength(4);
			entity.Property(a => a.Notes).HasMaxLength(500);

			entity.HasIndex(a => a.Registration).IsUnique();

			// Relation: Aircraft -> Airline (optional)
			entity.HasOne(a => a.Airline)
				.WithMany(al => al.Aircraft)
				.HasForeignKey(a => a.AirlineId)
				.OnDelete(DeleteBehavior.SetNull);

			entity.HasIndex(a => a.AirlineId);
		});

		// Configure Airline entity
		builder.Entity<Airline>(entity =>
		{
			entity.Property(a => a.IataCode).HasMaxLength(2);
			entity.Property(a => a.IcaoCode).HasMaxLength(3).IsRequired();
			entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
			entity.Property(a => a.Country).HasMaxLength(64).IsRequired();

			entity.HasIndex(a => a.IcaoCode).IsUnique();
			entity.HasIndex(a => a.IataCode).IsUnique().HasFilter("[IataCode] IS NOT NULL");
		});

		builder.Entity<Airport>(entity =>
		{
			entity.Property(a => a.IataCode).HasMaxLength(3);
			entity.Property(a => a.IcaoCode).HasMaxLength(4);
			entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
			entity.Property(a => a.City).HasMaxLength(64).IsRequired();
			entity.Property(a => a.Country).HasMaxLength(64).IsRequired();
			entity.Property(a => a.Latitude);
			entity.Property(a => a.Longitude);
			entity.Property(a => a.TimeZoneId).HasMaxLength(64);
			entity.HasIndex(a => a.IataCode).IsUnique().HasFilter("[IataCode] IS NOT NULL");
			entity.HasIndex(a => a.IcaoCode).IsUnique().HasFilter("[IcaoCode] IS NOT NULL");
		});

		// Configure UserFlight entity
		builder.Entity<UserFlight>(entity =>
		{
			entity.Property(uf => uf.SeatNumber).HasMaxLength(8).IsRequired();
			entity.Property(uf => uf.FlightClass).HasConversion<string>().IsRequired();
			entity.Property(uf => uf.Notes).HasMaxLength(500);

			entity.HasOne(uf => uf.Flight)
				  .WithMany(f => f.UserFlights)
				  .HasForeignKey(uf => uf.FlightId)
				  .OnDelete(DeleteBehavior.Cascade);

			// Configure relationship with ApplicationUser
			entity.HasOne<ApplicationUser>()
				  .WithMany(u => u.UserFlights)
				  .HasForeignKey(uf => uf.UserId)
				  .OnDelete(DeleteBehavior.Cascade);

			// Create composite index for performance
			entity.HasIndex(uf => new { uf.UserId, uf.FlightId });
			entity.HasIndex(uf => uf.BookedOnUtc);
		});
	}
}
