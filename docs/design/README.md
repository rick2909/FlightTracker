# Passport / Stats Design Reference

This folder contains a Stitch-style reference for the Passport / Stats screen layout.

- File: `passport-reference.png`
- Purpose: Visual guide for spacing, grouping, and composition (hero/profile area, routes map section, stat cards, and yearly chart rhythms).
- Usage: For design cues only. Do NOT render or ship this asset in the application UI.

Notes
- Keep SCSS tokens from `FlightTracker.Web/Styling/scss/_variables.scss` authoritative for color and spacing.
- When in doubt on paddings/margins, mirror the proportions from the reference (not exact pixels) to maintain visual balance.
- The map and chart should stretch fluidly on desktop; cards wrap at breakpoints as implemented in `_flight-stats.scss`.
