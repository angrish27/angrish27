# Scope

## Current state
- Layered backend with the URL shortener vertical slice (encode/decode, JSON file storage).
- Correlation id, CORS, global exception handling, health check.
- MSTest projects per layer with shared `TestBase`.
- Pulumi stack for Azure App Service + App Insights + Log Analytics.

## In scope (MVP - URL shortener)
| # | Capability | Status |
|---|------------|--------|
| 1 | Encode a long URL: `POST /api/url/encode` -> 201 new / 200 duplicate, response `{ shortUrl }`, `Location` header | Done |
| 2 | Validation: absolute http/https, max 2048 chars, no surrounding whitespace, no control chars, no embedded credentials, not this service -> 400 | Done |
| 3 | Sequential ids from 11157, Base62 codes (11157 = `2tx`), case-sensitive | Done |
| 4 | Decode `GET /{code}` -> 301 permanent redirect; unknown or malformed code -> 404 | Done |
| 5 | JSON file persistence (`UrlStore:FilePath`), loaded at startup; corrupt file blocks startup and is never overwritten | Done |
| 6 | Store behind `IUrlMappingStore` (single instance, see ADR-001) | Done |
| 7 | Rate limit on encode (429), 8 KB body limit, security headers, HSTS | Done |
| 8 | Unit + HTTP integration tests for every layer | Done |

## Deferred (not built; requires approval to add)
- Custom aliases, short URL details endpoint, delete, expiry (410), hit counts.
- Database-backed store (path described in ADR-001).

## Out of scope (unless approved)
- Frontend/UI.
- User accounts, authentication/authorization beyond reading existing claims.
- Advanced analytics (geo, referrer, device), QR codes, bulk import.
- Multi-region deployment, custom domains.

## Non-functional requirements
- .NET 10, warning-free build, all tests passing.
- Every request traceable via `x-correlation-id`.
- Validation errors return 400 with per-field error dictionary.
- No secrets in source control.

## Change control
Update the status column as features land. New scope items must be added here before implementation.
