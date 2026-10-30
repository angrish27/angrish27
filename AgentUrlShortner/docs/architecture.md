# Architecture

## Style
IDesign "Method"-style volatility-based decomposition. Each layer is a separate project inside a
numbered solution folder (the number reflects call direction: lower numbers call higher numbers).

## Layers
| # | Layer | Project | Responsibility | Visibility |
|---|-------|---------|----------------|------------|
| 1 | Clients | `CS.AgenticUrlShortner.WebApiClient` | HTTP endpoints, CORS, correlation, exception → HTTP mapping | public |
| 2 | Contracts | `Contracts.Client` | Public request/response DTOs exposed by Managers | public |
| 2 | Contracts | `Contracts.Logic` | Internal domain models passed between Manager/Engine/Accessor | public |
| 3 | Managers | `CS.AgenticUrlShortner.Managers` | Use-case orchestration, mapping Logic ↔ Client contracts | interfaces public, impl `internal sealed` |
| 4 | Engines | `CS.AgenticUrlShortner.Engines` | Business rules, validation, algorithms (e.g. short-code generation) | same |
| 5 | Accessors | `CS.AgenticUrlShortner.Accessors` | Persistence and external resources | same |
| 6 | Utilities | `CS.AgenticUrlShortner.Utilities` | Stateless helpers (`IIdentifierUtility`) | same |
| 7 | Common | `CS.AgenticUrlShortner.Common` | `IService`, `ServiceBase`, `FactoryBase`, `AmbientContext`, exceptions, `Constants` | public |
| 8 | Database | (reserved) | Future DB project / scripts | – |
| 9 | Tests | `Tests.*`, `Shared.Tests` | MSTest per layer | – |

## Call rules (MUST)
```
Client ──► Manager ──► Engine ──► Accessor
			   │          │          │
			   └──────────┴──────────┴──► Utility
All layers ──► Common, Contracts
```
- Clients call **only Managers** (via `ManagerFactory`).
- Managers may call Engines, Accessors, Utilities.
- Engines may call Accessors and Utilities. Engines never call Managers or other Engines.
- Accessors may call Utilities only. Accessors never call Engines/Managers or other Accessors.
- Managers never call other Managers synchronously.
- No upward calls. No sideways calls within the same layer.
- Client contracts (`Contracts.Client`) do not leak below Managers; Engines/Accessors work with `Contracts.Logic`.

## Project references
| Project | References |
|---------|------------|
| WebApiClient | Managers, Contracts.Client, Common |
| Managers | Engines, Accessors, Utilities, Contracts.Client, Contracts.Logic, Common |
| Engines | Accessors, Utilities, Contracts.Logic, Common |
| Accessors | Utilities, Contracts.Logic, Common |
| Utilities | Common |
| Common | Microsoft.Extensions.DependencyInjection.Abstractions |

## Building blocks
- **`IService`** – marker interface for every resolvable component (exposes `TestMe(string)`).
- **`ServiceBase`** – holds `Context` (`AmbientContext`) and `Factory` (`FactoryBase`).
- **Layer bases** – `ManagerBase` (EngineFactory, AccessorFactory, UtilityFactory),
  `EngineBase` (AccessorFactory, UtilityFactory), `AccessorBase` (UtilityFactory), `UtilityBase`.
  Bases are `internal abstract`; `InternalsVisibleTo` grants access to the layer's test project.
- **`FactoryBase`** – registers types (`TryAddTransient`) after validation (must implement `IService`,
  have a parameterless ctor) and resolves them from `IServiceProvider`.
- **Layer factories** – `ManagerFactory`, `EngineFactory`, `AccessorFactory`, `UtilityFactory` resolve
  the service and inject context + downstream factories. Downstream factories can be passed in
  explicitly (used by tests to inject fakes).

## Cross-cutting concerns
- **Correlation**: middleware reads/echoes `x-correlation-id` (falls back to `TraceIdentifier`).
- **Errors**: services throw `BadRequestException` / `AgenticUrlShortnerException`; the client's
  `ExceptionHandler` maps them to HTTP responses via a strategy dictionary. Unknown → 500.
- **CORS**: origins from config key `AllowedOrigins` (comma-separated).
- **Health**: `/health` (used by App Service health check).
- **Telemetry**: App Insights connection string injected by infra (`APPLICATIONINSIGHTS_CONNECTION_STRING`).
