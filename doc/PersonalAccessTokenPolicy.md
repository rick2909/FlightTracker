# Personal Access Token Policy

## Scope

This policy defines how user-managed personal access tokens are handled for first-party and third-party client access.

## Security Baseline

- Tokens are shown only once at creation time.
- Only token hashes are stored in persistent storage.
- Raw token values are never stored in the database.
- Revoked tokens are rejected immediately.
- Expired tokens are rejected immediately.

## Supported Scopes

- read:flights
- write:flights
- read:stats

## Operational Controls

- Token-authenticated calls are subject to rate limiting.
- Token usage is recorded through audit log events.
- Last-used timestamp is updated on successful token-authenticated requests.

## Future Stronger Option

PAT is the initial integration mode for external clients.
A stronger model is reserved for later: OAuth2/OIDC app registration with delegated flows.
