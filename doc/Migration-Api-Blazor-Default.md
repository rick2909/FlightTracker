# API + Blazor Default Architecture

## Status

FlightTracker now treats API + Blazor as the default presentation architecture.
Legacy MVC compatibility controllers remain temporarily available and are marked deprecated.

## Deprecated Legacy Controllers

- FlightTracker.Web/Controllers/Web/AirportsController.cs
- FlightTracker.Web/Controllers/Web/DashboardController.cs
- FlightTracker.Web/Controllers/Web/UserFlightsController.cs

These are compatibility paths during migration and should be removed in a follow-up cleanup once runtime parity is confirmed in production-like validation.

## Auth Default

- Web app authenticates users with cookie auth.
- Web typed API clients attach short-lived first-party bearer tokens.
- API validates bearer tokens and enforces user-route ownership checks.

## Routing Default

- Browser UI uses FlightTracker.Web (Blazor Server host) as the default frontend.
- Data access and user-scoped operations should flow through FlightTracker.Api versioned routes.

## Scope Guardrail

This migration step changes architecture defaults and safety controls only.
No new product/business features are introduced in this step.
