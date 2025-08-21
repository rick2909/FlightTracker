using FlightTracker.Application.Dtos;
using FlightTracker.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Application.Services.Interfaces;

/// <summary>
/// Service interface for managing user flight experiences.
/// </summary>
public interface IUserFlightService
{
    /// <summary>
    /// Gets all flights for a specific user.
    /// </summary>
    Task<IEnumerable<UserFlightDto>> GetUserFlightsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user flight by ID.
    /// </summary>
    Task<UserFlightDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flights for a user filtered by flight class.
    /// </summary>
    Task<IEnumerable<UserFlightDto>> GetUserFlightsByClassAsync(int userId, FlightClass flightClass, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user flight experience.
    /// </summary>
    Task<UserFlightDto> AddUserFlightAsync(int userId, CreateUserFlightDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user flight experience.
    /// </summary>
    Task<UserFlightDto?> UpdateUserFlightAsync(int id, CreateUserFlightDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates both the user flight fields and underlying flight schedule/route in one operation.
    /// </summary>
    Task<UserFlightDto?> UpdateUserFlightAndScheduleAsync(
        int id,
        UpdateUserFlightDto userFlight,
        FlightScheduleUpdateDto schedule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user flight experience.
    /// </summary>
    Task<bool> DeleteUserFlightAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets flight statistics for a user.
    /// </summary>
    Task<UserFlightStatsDto> GetUserFlightStatsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has already recorded a specific flight.
    /// </summary>
    Task<bool> HasUserFlownFlightAsync(int userId, int flightId, CancellationToken cancellationToken = default);
}
