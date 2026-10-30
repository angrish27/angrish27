# Greenfield Scenario - URL Shortener Feature

Goal: build a new capability from a spec - encode a long URL to a short Base62 code (sequential ids from 11157 = 2tx), decode it with a 302 redirect, persist to a JSON file.

## 1. Decomposition
The spec was split into single-responsibility units, one per IDesign layer, built bottom-up so each layer is testable before the next depends on it.

| # | Unit | Layer | Responsibility |
|---|---|---|---|
| 1 | Contracts: EncodeUrlRequest, EncodeUrlResponse, DecodeUrlResponse, UrlMapping | Contracts | Wire and storage shapes (response serializes shortUrl only) |
| 2 | IBase62Utility / Base62Utility | Utility | Encode(long) and IsValid(code) - pure, stateless |
| 3 | IUrlValidationEngine / UrlValidationEngine | Engine | Rules: required, max 2048, no surrounding whitespace, absolute http/https |
| 4 | UrlMappingStore (singleton) | Accessor | Load JSON file, in-memory lookups, sequential ids, atomic write (.tmp + move), refuse corrupt file |
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
        UrlMappingStore: lock, next seq, UtilityFactory -> IBase62Utility.Encode, persist, publish (new -> 201)

HTTP GET {code}
  ProcessUrlController.DecodeUrl
    IUrlManager.Decode -> IBase62Utility.IsValid -> IUrlMappingAccessor.FindByCode
      found -> 302 Location: longUrl ; else 404

Startup.Configure -> ManagerFactory.Initialize -> AccessorFactory.Initialize -> UrlMappingStore.Load(path)
```

Build order followed: Contracts, Utility, Engine, Store, Accessor, Manager, Controller, Startup wiring - each step built and tested before the next.

## 3. Validation
Each unit has tests at its own layer, plus end-to-end HTTP tests.

| Unit | Tests |
|---|---|
| Base62Utility | Base62Utility_Encode_ReturnsExpected (11157 = 2tx), Base62Utility_IsValid_ReturnsExpected |
| UrlValidationEngine | UrlValidationEngine_ValidUrl_DoesNotThrow, UrlValidationEngine_InvalidUrl_ThrowsBadRequest |
| UrlMappingStore | FirstMapping_Gets11157AndIsPersisted, Duplicate_ReturnsExistingWithoutNewId, ExactStringDeterminesDuplicates, Restart_LoadsMappingsAndContinuesSequence, ConcurrentSameUrl_CreatesSingleMapping, CorruptFile_ThrowsAndLeavesFileUntouched |
| UrlManager | Encode_NewThenDuplicate, Encode_InvalidUrl_ThrowsBadRequest, Decode_ReturnsLongUrlOrNull |
| End-to-end (ApiTests) | 201+Location, 2tx then 2ty, duplicate 200, invalid 400, missing body 400, decode 302, unknown 404, restart, corrupt store |

Run: `dotnet test CS.AgenticUrlShortner.sln` - 53 passed, 0 failed.

## Spec coverage
| Requirement | Evidence |
|---|---|
| Sequential ids starting 11157 (2tx) | Base62Utility_Encode_ReturnsExpected, EncodeUrl_SequentialUrls_Return2txThen2ty |
| JSON file persistence across restarts | UrlMappingStore_Restart_..., Restart_LoadsPersistedMappingsAndContinuesSequence |
| 201 new / 200 duplicate | EncodeUrl_NewUrl_..., EncodeUrl_Duplicate_Returns200WithSameShortUrl |
| 302 redirect / 404 unknown | DecodeUrl_KnownCode_..., DecodeUrl_UnknownCode_Returns404 |
| 400 invalid input | UrlValidationEngine_InvalidUrl_..., EncodeUrl_InvalidUrl_..., EncodeUrl_MissingBody_Returns400 |
| Corrupt storage not overwritten | UrlMappingStore_CorruptFile_..., Startup_CorruptStore_FailsAndLeavesFileUntouched |

## Verdict
Met: decomposition, orchestration and validation are documented and every spec requirement maps to a passing test.
