# Architecture Decision Records

## ADR-001: JSON file store, single instance, behind IUrlMappingStore

**Status:** Accepted

### Context
The spec requires mappings to persist in a JSON file, sequential ids starting at 11157, no duplicate codes, and that a corrupt file is never overwritten.

### Decision
- Persistence sits behind `IUrlMappingStore` (Accessors/Url). `UrlMappingAccessor` depends only on the interface.
- Default implementation `UrlMappingStore`: loaded once at startup, in-memory lookups, `SemaphoreSlim` serialising writes, atomic save (`.tmp` then `File.Move`).
- Registered as a singleton via `TryAddSingleton<IUrlMappingStore, UrlMappingStore>()` in `AccessorFactory.RegisterTypes`.

### Consequences / limits
- **Single instance only.** The lock and the sequence counter are per process. Two instances sharing a file would issue duplicate ids and overwrite each other.
- Every write rewrites the whole file: O(n) per encode. Fine for thousands of mappings, not millions.
- Memory holds every mapping.
- Rate limiting (see security.md) is also per instance.

### Scale-out path
1. Implement `IUrlMappingStore` over a database (e.g. Azure SQL / PostgreSQL with an identity column seeded at 11157 and unique indexes on long URL and code, or Cosmos DB with a counter document).
2. Register it before `AccessorFactory.RegisterTypes` (TryAdd keeps the first registration) or replace the line in `RegisterTypes`. `Load(location)` receives the connection string.
3. Move the duplicate check into the database (unique constraint + insert-or-get) instead of the in-process lock.
4. Optionally add a read cache (IMemoryCache / Redis) for `FindByCode`.
5. Use a distributed rate limiter or gateway throttling.

No changes are needed in managers, engines, controller or contracts.

### Evidence
- `UrlMappingStore_DefaultRegistration_IsJsonFileStore`
- `UrlMappingAccessor_UsesReplacementStore_WhenRegisteredFirst` (a fake store is used without code changes)
- Store behaviour: `UrlMappingStoreTests` (concurrency, restart, corrupt file)
