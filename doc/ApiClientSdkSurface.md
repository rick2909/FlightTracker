# FlightTracker API Minimal Client SDK Surface

## Purpose

This document defines the minimum typed client surface that first-party clients should implement for API reuse.

## Base Address And Headers

- Base URL: [https://api-host/api/v1/](https://api-host/api/v1/)
- Required auth header for secured calls: Authorization: Bearer {token}
- Recommended default header: Accept: application/json

## Endpoint Groups

### Airports

- GET /airports
- GET /airports/{code}
- GET /airports/{code}/flights

### Flights

- GET /flights/{id}
- GET /flights/upcoming?fromUtc={iso-8601}&windowHours={int}

### Passport

- GET /passport/users/{userId}
- GET /passport/users/{userId}/details

### Stats

- GET /stats/users/{userId}/passport-details

### Preferences

- GET /preferences/users/{userId}
- PUT /preferences/users/{userId}

### User Flights

- GET /users/{userId}/flights
- GET /users/{userId}/flights/stats
- GET /users/{userId}/flights/{flightId}/has-flown
- POST /users/{userId}/flights
- GET /user-flights/{id}
- PUT /user-flights/{id}
- DELETE /user-flights/{id}

### Personal Access Tokens

- GET /users/{userId}/access-tokens
- POST /users/{userId}/access-tokens
- POST /users/{userId}/access-tokens/revoke

## Contract Source Of Truth

DTO contracts for API evolution are defined in:

- FlightTracker.Api/Contracts/V1

Application DTOs used by services are defined in:

- FlightTracker.Application/Dtos

## Suggested Client Interfaces

- IAirportsApiClient
- IFlightsApiClient
- IPassportApiClient
- IStatsApiClient
- IUserPreferencesApiClient
- IUserFlightsApiClient

## Evolution Guidance

- Keep client interfaces stable for each major API version.
- Add optional members for additive changes.
- For breaking changes, publish parallel v2 client interfaces before removing v1 interfaces.
