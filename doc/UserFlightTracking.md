# User Flight Tracking Feature

This document describes the user flight tracking functionality added to the FlightTracker application.

## Overview

The system now tracks which users have taken which flights, including detailed information about their flight experience such as class of service, seat assignment, and personal notes.

## Domain Entities

### UserFlight
Represents a user's specific flight experience, tracking the relationship between users and flights.

**Properties:**
- `Id`: Unique identifier
- `UserId`: Reference to the user (ApplicationUser)
- `FlightId`: Reference to the flight
- `FlightClass`: Enum (Economy, PremiumEconomy, Business, First)
- `SeatNumber`: String representation (e.g., "12A", "1B")
- `BookedOnUtc`: When the user recorded this flight
- `Notes`: Optional user notes about the experience
- `DidFly`: Boolean indicating if the user actually flew

### Flight (Updated)
- Added `UserFlights` navigation property
- Changed `Status` from string to `FlightStatus` enum

### ApplicationUser (Updated)
- Added `UserFlights` navigation property

## Enums

### FlightClass
- Economy
- PremiumEconomy
- Business
- First

### FlightStatus
- Scheduled
- Delayed
- Boarding
- Departed
- InFlight
- Landed
- Cancelled
- Diverted

## Repository Layer

### IUserFlightRepository
Provides data access methods for user flight operations:
- Get user flights (all, by class, by flight)
- Add/Update/Delete user flights
- Get flight statistics
- Check if user has flown a specific flight

### UserFlightRepository
Entity Framework implementation with proper include statements for related data.

## Application Layer

### DTOs
- `UserFlightDto`: Complete user flight information with flattened flight/airport details
- `CreateUserFlightDto`: For creating/updating user flights
- `UserFlightStatsDto`: User flight statistics

### IUserFlightService
Business logic for user flight operations:
- Validation (flight exists, user hasn't already recorded the flight)
- Mapping between entities and DTOs
- Statistics calculation

## Database Configuration

### Entity Relationships
- UserFlight -> Flight (Many-to-One, Cascade Delete)
- UserFlight -> ApplicationUser (Many-to-One, Cascade Delete)
- Composite index on (UserId, FlightId) for performance
- Index on BookedOnUtc for chronological queries

### Seed Data
Includes sample user flights for demo and admin users with various flight classes and realistic flight experiences.

## Usage Examples

### Track a New Flight
```csharp
var createDto = new CreateUserFlightDto
{
    FlightId = 1,
    FlightClass = FlightClass.Business,
    SeatNumber = "3A",
    Notes = "Great service, good meal",
    DidFly = true
};

var userFlight = await userFlightService.AddUserFlightAsync(userId, createDto);
```

### Get User's Flight History
```csharp
var userFlights = await userFlightService.GetUserFlightsAsync(userId);
```

### Get User Statistics
```csharp
var stats = await userFlightService.GetUserFlightStatsAsync(userId);
// Returns total flights, flights by class, unique airports/countries
```

## Next Steps for Web Implementation

1. **Controllers**: Create MVC and API controllers for user flight management
2. **Views**: Create pages for:
   - User flight history
   - Add new flight experience
   - Flight statistics dashboard
   - Edit flight details
3. **Authorization**: Ensure users can only manage their own flights
4. **Validation**: Add client-side and server-side validation
5. **Search/Filtering**: Add search by date, airline, route, etc.

## Clean Architecture Benefits

- **Domain**: Pure business entities with no external dependencies
- **Infrastructure**: Database-specific implementation details
- **Application**: Business logic and DTOs for data transfer
- **Separation of Concerns**: Each layer has a single responsibility
- **Testability**: Easy to unit test business logic independently
