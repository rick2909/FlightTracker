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

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		// Configure Flight entity
		builder.Entity<Flight>(entity =>
		{
			entity.Property(f => f.FlightNumber).HasMaxLength(16).IsRequired();
			entity.Property(f => f.Status).HasMaxLength(32).IsRequired();

			entity.HasOne(f => f.DepartureAirport)
				  .WithMany(a => a.DepartingFlights)
				  .HasForeignKey(f => f.DepartureAirportId)
				  .OnDelete(DeleteBehavior.Restrict);

			entity.HasOne(f => f.ArrivalAirport)
				  .WithMany(a => a.ArrivingFlights)
				  .HasForeignKey(f => f.ArrivalAirportId)
				  .OnDelete(DeleteBehavior.Restrict);
		});

		builder.Entity<Airport>(entity =>
		{
			entity.Property(a => a.Code).HasMaxLength(8).IsRequired();
			entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
			entity.Property(a => a.City).HasMaxLength(64).IsRequired();
			entity.Property(a => a.Country).HasMaxLength(64).IsRequired();
			entity.HasIndex(a => a.Code).IsUnique();
		});
	}
}
