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

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		// Configure Flight entity
		builder.Entity<Flight>(entity =>
		{
			entity.Property(f => f.FlightNumber).HasMaxLength(16).IsRequired();
			entity.Property(f => f.Status).HasConversion<string>().IsRequired();

			entity.HasOne(f => f.DepartureAirport)
				  .WithMany(a => a.DepartingFlights)
				  .HasForeignKey(f => f.DepartureAirportId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(f => f.ArrivalAirport)
				  .WithMany(a => a.ArrivingFlights)
				  .HasForeignKey(f => f.ArrivalAirportId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasIndex(f => f.FlightNumber);
		});

		builder.Entity<Airport>(entity =>
		{
			entity.Property(a => a.Code).HasMaxLength(8).IsRequired();
			entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
			entity.Property(a => a.City).HasMaxLength(64).IsRequired();
			entity.Property(a => a.Country).HasMaxLength(64).IsRequired();
			entity.HasIndex(a => a.Code).IsUnique();
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
