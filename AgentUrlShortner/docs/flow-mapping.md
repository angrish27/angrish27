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
| POST | `/api/url/encode` | `EncodeUrl` | `IUrlManager.EncodeUrlAsync` | `IUrlValidationEngine` | `IUrlMappingAccessor` | `IBase62Utility` | 201 new (Location = short URL), 200 duplicate, 400 invalid |
| GET | `/{code}` | `DecodeUrl` | `IUrlManager.DecodeUrlAsync` | – | `IUrlMappingAccessor.FindByCode` | `IBase62Utility.IsValid` | 302 (Location = long URL), 404 unknown |

### Encode
```
ValidateLongUrl (absolute http/https, <= 2048, no surrounding whitespace) → 400 on failure
FindByLongUrl (in-memory, exact ordinal string) → hit: return existing, 200, no storage access
GetOrCreateAsync → UrlMappingStore.GetOrAddAsync:
   acquire SemaphoreSlim (serialized creation)
   recheck duplicate inside lock → 200
   seq = next (11157 first, else max persisted + 1); code = Base62(seq)
   write full JSON to <file>.tmp → File.Move over <file>   (persist BEFORE publish)
   publish to code→url and url→code caches; seq++ → 201
```

### Store rules (`UrlMappingStore`, singleton)
- File: config `UrlStore:FilePath` (default `App_Data/url-mappings.json`, relative to content root).
- Loaded once at startup via `ManagerFactory.Initialize` → `AccessorFactory.Initialize`.
- Missing file = empty store. Invalid JSON/empty/null/duplicate or invalid records → `AgenticUrlShortnerException`, app fails to start, file never overwritten.
- Records contain exactly `seqNumber`, `actualLongURL`, `base62` (no analytics fields).
- Single instance assumption; no external writers.
