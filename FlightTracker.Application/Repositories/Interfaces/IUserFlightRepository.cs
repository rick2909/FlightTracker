using FlightTracker.Domain.Entities;
using FlightTracker.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Repositories.Interfaces;

/// <summary>
/// Repository interface for managing user flight experiences.
/// </summary>
public interface IUserFlightRepository
{
    /// <summary>
    /// Gets all flights for a specific user.
    /// </summary>
    Task<IEnumerable<UserFlight>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user flight by ID.
    /// </summary>
    Task<UserFlight?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flights for a user filtered by flight class.
    /// </summary>
    Task<IEnumerable<UserFlight>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users who flew on a specific flight.
    /// </summary>
    Task<IEnumerable<UserFlight>> GetFlightPassengersAsync(int flightId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user flight experience.
    /// </summary>
    Task<UserFlight> AddAsync(UserFlight userFlight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user flight experience.
    /// </summary>
    Task<UserFlight> UpdateAsync(UserFlight userFlight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user flight experience.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flight statistics for a user (total flights, miles, etc.).
    /// </summary>
    Task<int> GetUserFlightCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has already recorded a specific flight.
    /// </summary>
    Task<bool> HasUserFlownFlightAsync(int userId, int flightId, CancellationToken cancellationToken = default);
}
