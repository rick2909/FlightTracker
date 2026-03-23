# API Slice Parity Checklist

## Airports

- [x] `Airports/Browse` preserves the existing JSON shape used by the map (`id`, `name`, `city`, `country`, `iata`, `icao`, `lat`, `lon`)
- [x] `Airports/{id}/Flights` preserves the existing JSON shape used by the map (`departing`, `arriving`, `route`, `flightNumber`, `airline`, `aircraft`)
- [x] Airport lookup still resolves by existing airport id before requesting flights
- [x] `live` toggle is forwarded unchanged to the API-backed slice
- [x] `404` is still returned when an airport id cannot be resolved
- [x] `500` still returns the same error envelope shape (`{ error: string }`)
- [x] Rollout is gated by configuration so the in-process implementation remains available during migration
