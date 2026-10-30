# Greenfield Scenario - URL Shortener Feature

Goal: build a URL shortener that generates URL-safe Base64 short codes from request-received Unix epoch nanoseconds, decodes them with a 302 redirect, and persists mappings to a JSON file.

## 1. Decomposition
The spec was split into single-responsibility units, one per IDesign layer, built bottom-up so each layer is testable before the next depends on it.

| # | Unit | Layer | Responsibility |
|---|---|---|---|
| 1 | Contracts: EncodeUrlRequest, EncodeUrlResponse, DecodeUrlResponse, UrlMapping | Contracts | Wire and storage shapes (response serializes shortUrl only) |
| 2 | IShortCodeUtility / ShortCodeUtility | Utility | Convert UTC time to epoch nanoseconds, encode/decode URL-safe Base64 tokens, and validate current/legacy codes |
| 3 | IUrlValidationEngine / UrlValidationEngine | Engine | Rules: required, max 2048, no surrounding whitespace, absolute http/https |
| 4 | UrlMappingStore (singleton) | Accessor | Load JSON file, in-memory lookups, monotonic timestamp allocation, atomic write (.tmp + move), refuse corrupt file |
| 5 | IUrlMappingAccessor / UrlMappingAccessor | Accessor | FindByLongUrl, FindByCode, GetOrCreateAsync over the store |
| 6 | IUrlManager / UrlManager | Manager | Use-case flow: validate, find existing, else create; decode code to long URL |
| 7 | ProcessUrlController | Client | POST api/url/encode (201/200 + Location), GET {code} (302/404) |
| 8 | Startup + appsettings UrlStore:FilePath | Client/Common | Load store once at startup via ManagerFactory.Initialize |

## 2. Orchestration
Runtime call chain (all services created via factories, never new):

```
HTTP POST api/url/encode
  ProcessUrlController.EncodeUrl
    ManagerFactory -> IUrlManager.EncodeAsync
      EngineFactory  -> IUrlValidationEngine.Validate      (400 on failure)
      AccessorFactory -> IUrlMappingAccessor.FindByLongUrl  (existing -> 200)
      AccessorFactory -> IUrlMappingAccessor.GetOrCreateAsync
        UrlMappingStore: lock, allocate unique epoch nanoseconds, UtilityFactory -> IShortCodeUtility, persist, publish (new -> 201)

HTTP GET {code}
  ProcessUrlController.DecodeUrl
    IUrlManager.Decode -> IShortCodeUtility.IsValid -> IUrlMappingAccessor.FindByCode
      found -> 302 Location: longUrl ; else 404

Startup.Configure -> ManagerFactory.Initialize -> AccessorFactory.Initialize -> UrlMappingStore.Load(path)
```

Build order followed: Contracts, Utility, Engine, Store, Accessor, Manager, Controller, Startup wiring - each step built and tested before the next.

## 3. Validation
Each unit has tests at its own layer, plus end-to-end HTTP tests.

| Unit | Tests |
|---|---|
| ShortCodeUtility | ShortCodeUtility_EncodeEpochNanoseconds_ReturnsBase64Url, ShortCodeUtility_TimestampRoundTripsAtNanosecondUnitResolution, ShortCodeUtility_IsValid_AcceptsCurrentAndLegacyCodes |
| UrlValidationEngine | UrlValidationEngine_ValidUrl_DoesNotThrow, UrlValidationEngine_InvalidUrl_ThrowsBadRequest |
| UrlMappingStore | UrlMappingStore_FirstMapping_UsesRequestEpochAndIsPersisted, UrlMappingStore_Duplicate_ReturnsExistingWithoutNewId, UrlMappingStore_ExactStringDeterminesDuplicates, UrlMappingStore_Restart_LoadsMappingsAndContinuesSequence, UrlMappingStore_ConcurrentSameUrl_CreatesSingleMapping, UrlMappingStore_LoadsLegacyBase62MappingsAndKeepsThemResolvable, UrlMappingStore_CorruptFile_ThrowsAndLeavesFileUntouched |
| UrlManager | Encode_NewThenDuplicate, Encode_InvalidUrl_ThrowsBadRequest, Decode_ReturnsLongUrlOrNull |
| End-to-end (ApiTests) | 201+Location with epoch token, distinct codes, duplicate 200, invalid 400, missing body 400, decode 302, legacy code decode, unknown 404, restart, corrupt store |

Run: `dotnet test CS.AgenticUrlShortner.sln` - 53 passed, 0 failed.

## Spec coverage
| Requirement | Evidence |
|---|---|
| URL-safe Base64 code contains request-received epoch nanoseconds | ShortCodeUtility round-trip tests, EncodeUrl_NewUrl_Returns201WithLocationAndOnlyShortUrl |
| Unique codes despite same clock tick and across restart | UrlMappingStore_ConcurrentSameUrl_CreatesSingleMapping, UrlMappingStore_Restart_LoadsMappingsAndContinuesSequence, Restart_LoadsPersistedMappingsAndContinuesEpochCodes |
| JSON file persistence across restarts | UrlMappingStore_Restart_..., Restart_LoadsPersistedMappingsAndContinuesSequence |
| 201 new / 200 duplicate | EncodeUrl_NewUrl_..., EncodeUrl_Duplicate_Returns200WithSameShortUrl |
| 302 redirect / 404 unknown | DecodeUrl_KnownCode_..., DecodeUrl_UnknownCode_Returns404 |
| 400 invalid input | UrlValidationEngine_InvalidUrl_..., EncodeUrl_InvalidUrl_..., EncodeUrl_MissingBody_Returns400 |
| Corrupt storage not overwritten | UrlMappingStore_CorruptFile_..., Startup_CorruptStore_FailsAndLeavesFileUntouched |

## Verdict
Met: decomposition, orchestration and validation are documented and every spec requirement maps to a passing test.
