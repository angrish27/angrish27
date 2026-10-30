# Flow Mapping

Each endpoint maps to exactly one Manager method. Add a row/section for every new endpoint.

## Endpoint map
| HTTP | Route | Controller.Action | Manager.Method | Engines | Accessors | Utilities | Success | Errors |
|------|-------|-------------------|----------------|---------|-----------|-----------|---------|--------|
| GET | `/health` | health checks | – | – | – | – | 200 | – |

## Error flow
```
throw in any layer → bubbles to UseExceptionHandler
 → ExceptionHandler.Strategy[exception type] → (status, body)  [logged Warning]
 → no match → 500 [logged Error; details only in Development]
```

## URL shortener (implemented) – `ProcessUrlController`
| HTTP | Route | Action | Manager | Engines | Accessors | Utilities | Responses |
|------|-------|--------|---------|---------|-----------|-----------|-----------|
| POST | `/api/url/encode` | `EncodeUrl` | `IUrlManager.EncodeUrlAsync` | `IUrlValidationEngine` | `IUrlMappingAccessor` | `IShortCodeUtility` | 201 new (Location = short URL), 200 duplicate, 400 invalid |
| GET | `/{code}` | `DecodeUrl` | `IUrlManager.DecodeUrlAsync` | – | `IUrlMappingAccessor.FindByCode` | `IShortCodeUtility.IsValid` | 302 (Location = long URL), 404 unknown |

### Encode
```
Controller captures request-received UTC timestamp; manager converts ticks to Unix epoch nanoseconds
ValidateLongUrl (absolute http/https, <= 2048, no surrounding whitespace) → 400 on failure
FindByLongUrl (in-memory, exact ordinal string) → hit: return existing, 200, no storage access
GetOrCreateAsync → UrlMappingStore.GetOrAddAsync:
   acquire SemaphoreSlim (serialized creation)
   recheck duplicate inside lock → 200
   code timestamp = max(request epoch nanoseconds, last persisted code timestamp + 1)
   shortCode = URL-safe Base64 of the big-endian 64-bit timestamp
   write full JSON to <file>.tmp → File.Move over <file>   (persist BEFORE publish)
   publish to code→url and url→code caches; record sequence id++ → 201
```

### Store rules (`UrlMappingStore`, singleton)
- File: config `UrlStore:FilePath` (default `App_Data/url-mappings.json`, relative to content root).
- Loaded once at startup via `ManagerFactory.Initialize` → `AccessorFactory.Initialize`.
- Missing file = empty store. Invalid JSON/empty/null/duplicate or invalid records → `AgenticUrlShortnerException`, app fails to start, file never overwritten.
- New records contain `seqNumber`, `actualLongURL`, `shortCode`, and `codeEpochNanoseconds`. Existing `base62` records are migrated when the store is next written; their short codes continue to resolve.
- Single instance assumption; no external writers.

The code timestamp is expressed in nanoseconds since the Unix epoch, but `DateTimeOffset` supplies 100-nanosecond ticks. The store advances timestamps monotonically under its write lock to avoid collisions when multiple requests share a clock tick or the wall clock moves backwards. The JSON store remains single-instance only.
