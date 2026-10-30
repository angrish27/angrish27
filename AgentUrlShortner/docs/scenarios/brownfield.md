# Brownfield Scenario - Validation Evidence

## Change to existing skeleton
- Placeholder controller replaced by ProcessUrlController (EncodeUrl / DecodeUrl); placeholder slice removed.
- New services registered in existing Utility/Engine/Accessor/Manager factories.
- Store loaded at startup via ManagerFactory.Initialize in Startup.Configure.

## Reused unchanged
- Factory chain, AmbientContext, correlation-id middleware, exception handlers, /health.

## Validation
HTTP-level tests: 9. Tests/CS.AgenticUrlShortner.Tests.ApiTests (WebApplicationFactory<Startup>, isolated temp store per test).

| Test | Covers |
|---|---|
| EncodeUrl_NewUrl_Returns201WithLocationAndOnlyShortUrl | 201, Location header, body = shortUrl only |
| EncodeUrl_DifferentUrls_ReturnUniqueEpochCodes | distinct request timestamps produce increasing short codes |
| EncodeUrl_Duplicate_Returns200WithSameShortUrl | 200 on duplicate |
| EncodeUrl_InvalidUrl_Returns400AndDoesNotPersist | 400, no store write |
| EncodeUrl_MissingBody_Returns400 | 400 |
| DecodeUrl_KnownCode_Returns302WithLongUrlLocation | 302 + Location |
| DecodeUrl_UnknownCode_Returns404 | 404 |
| Restart_LoadsPersistedMappingsAndContinuesSequence | startup load regression |
| Startup_CorruptStore_FailsAndLeavesFileUntouched | corrupt store not overwritten |
| ExistingPipeline_HealthAndCorrelationHeader_StillWork | existing pipeline unbroken |

Run: `dotnet test CS.AgenticUrlShortner.sln` - 53 passed, 0 failed (14 API, 39 unit).

## Verdict
Met: HTTP layer and startup regression are covered by automated tests.
