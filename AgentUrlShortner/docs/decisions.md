# Architecture Decision Records

## ADR-001: JSON file store, single instance, behind IUrlMappingStore

**Status:** Accepted

### Context
Mappings persist in a JSON file, new short codes use URL-safe Base64 of request-received Unix epoch nanoseconds, duplicate URLs reuse their existing code, and a corrupt file is never overwritten.

### Decision
- Persistence sits behind `IUrlMappingStore` (Accessors/Url). `UrlMappingAccessor` depends only on the interface.
- Default implementation `UrlMappingStore`: loaded once at startup, in-memory lookups, `SemaphoreSlim` serialising writes and monotonically assigning unique code timestamps, atomic save (`.tmp` then `File.Move`).
- Existing `base62` mappings are read as legacy short codes and continue to redirect; the next successful write migrates them to the `shortCode` JSON property.
- Registered as a singleton via `TryAddSingleton<IUrlMappingStore, UrlMappingStore>()` in `AccessorFactory.RegisterTypes`.

### Consequences / limits
- **Single instance only.** The lock and monotonic timestamp state are per process. Two instances sharing a file could generate conflicting codes or overwrite each other's changes.
- Every write rewrites the whole file: O(n) per encode. Fine for thousands of mappings, not millions.
- Memory holds every mapping.
- Rate limiting (see security.md) is also per instance.

### Scale-out path
1. Implement `IUrlMappingStore` over a database (e.g. Azure SQL / PostgreSQL with unique indexes on long URL and code, or Cosmos DB with conditional writes).
2. Register it before `AccessorFactory.RegisterTypes` (TryAdd keeps the first registration) or replace the line in `RegisterTypes`. `Load(location)` receives the connection string.
3. Allocate a unique timestamp/code atomically in the database, and move the duplicate check into the database (unique constraint + insert-or-get) instead of the in-process lock.
4. Optionally add a read cache (IMemoryCache / Redis) for `FindByCode`.
5. Use a distributed rate limiter or gateway throttling.

No changes are needed in managers, engines, controller or contracts.

### Evidence
- `UrlMappingStore_DefaultRegistration_IsJsonFileStore`
- `UrlMappingAccessor_UsesReplacementStore_WhenRegisteredFirst` (a fake store is used without code changes)
- Store behaviour: `UrlMappingStoreTests` (concurrency, restart, corrupt file)
