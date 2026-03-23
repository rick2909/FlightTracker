# API Slice Parity Checklist

## Airports

- [x] `Airports/Browse` preserves the existing JSON shape used by the map (`id`, `name`, `city`, `country`, `iata`, `icao`, `lat`, `lon`)
- [x] `Airports/{id}/Flights` preserves the existing JSON shape used by the map (`departing`, `arriving`, `route`, `flightNumber`, `airline`, `aircraft`)
- [x] Airport lookup still resolves by existing airport id before requesting flights
- [x] `live` toggle is forwarded unchanged to the API-backed slice
- [x] `404` is still returned when an airport id cannot be resolved
- [x] `500` still returns the same error envelope shape (`{ error: string }`)
- [x] Rollout is gated by configuration so the in-process implementation remains available during migration

## Flights

- [x] User flight list/details/stats can be sourced from `/api/v1/users/{userId}/flights*` via typed Web clients
- [x] Add/edit/delete flows use the same `IUserFlightService` contract from the Web layer with API fallback behind DI
- [x] Existing controllers and Razor components keep their current behavior and validation handling
- [x] Rollout is gated by `FlightTrackerApi:Slices:Flights`

## Passport

- [x] Passport aggregates and details can be sourced from typed API clients
- [x] Passport page still combines aggregate data, preferences, and user-flight filtering with the same controller logic
- [x] Rollout is gated by `FlightTrackerApi:Slices:Passport`

## Settings

- [x] Preferences read/write can be sourced from typed API clients
- [x] Flight export/delete operations flow through the API-backed user-flight service when the Flights slice is enabled
- [x] Profile/password management intentionally remains local during the transition window because those flows are still Web + Identity specific
- [x] Rollout is gated by `FlightTrackerApi:Slices:Settings`
