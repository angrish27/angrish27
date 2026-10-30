# Data Models

Conventions: plain POCO classes, `{ get; set; }` properties, no attributes, nullable reference types
disabled, one type per file, file name = type name. Timestamps are UTC and suffixed `Utc`.

## Client contracts (`2. Contracts/CS.AgenticUrlShortner.Contracts.Client`)
Exposed by Managers and serialized by the Web API (camelCase JSON).

## Logic contracts (`2. Contracts/CS.AgenticUrlShortner.Contracts.Logic`)
Used between Managers, Engines and Accessors. Never returned to clients directly.

## Mapping
Managers own mapping between Logic and Client contracts (private static `ToResponse(...)`-style methods).

## Ambient context (`7. Common/.../Contracts/AmbientContext.cs`)
| Property | Type | Source |
|----------|------|--------|
| CorrelationId | string | `x-correlation-id` header or `TraceIdentifier` |
| UserId | string | `HttpContext.User.Identity.Name` |
| FullName | string | (not populated yet) |
| Groups | string[] | (not populated yet) |
| Claims | Claim[] | `HttpContext.User.Claims` |

## Exceptions (`7. Common/.../Exceptions`)
| Exception | HTTP | Usage |
|-----------|------|-------|
| `BadRequestException(message, errors?)` | 400 – body `{ error, errors }` | Validation failures; `errors` is `Dictionary<string,string[]>` keyed by property |
| `AgenticUrlShortnerException` | its `StatusCode` – body `{ error }` | Domain/application errors |
| any other | 500 | Details only in Development |

## URL shortener models
| Model | Layer | Fields |
|-------|-------|--------|
| `EncodeUrlRequest` | Client | `LongUrl` |
| `EncodeUrlResponse` | Client | Serialized: `ShortUrl` only. Internal (`[JsonIgnore]`): `LongUrl`, `Code`, `IsNew` (drives 201/200) |
| `DecodeUrlResponse` | Client | `LongUrl` |
| `UrlMapping` | Logic / persisted | `SeqNumber` → `seqNumber`, `ActualLongUrl` → `actualLongURL`, `ShortCode` → `shortCode`, `CodeEpochNanoseconds` → `codeEpochNanoseconds` |

New persisted records contain those four fields. The reader also accepts the legacy `base62` property and preserves those codes for existing links; a subsequent store write serializes them as `shortCode`. Analytics must be a separate increment – do not add fields to `UrlMapping`.

New short codes are URL-safe Base64 (RFC 4648 URL alphabet without padding) of an 8-byte big-endian Unix-epoch-nanosecond timestamp. `DateTimeOffset` has 100-nanosecond tick precision; the store monotonically advances the timestamp for uniqueness within one process. Legacy Base62 codes remain valid for mappings loaded from the old JSON format.
