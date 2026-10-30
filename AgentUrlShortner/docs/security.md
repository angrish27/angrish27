# Security

| Threat | Control | Where | Test |
|---|---|---|---|
| Malicious schemes (javascript:, data:, ftp:) | Only absolute http/https accepted | UrlValidationEngine | UrlValidationEngine_InvalidUrl_ThrowsBadRequest |
| Phishing via embedded credentials (https://google.com@evil) | Reject URLs with user info | UrlValidationEngine | UrlValidationEngine_InvalidUrl_ThrowsBadRequest |
| Header / log injection | Reject control characters and surrounding whitespace | UrlValidationEngine | UrlValidationEngine_InvalidUrl_ThrowsBadRequest |
| Redirect loops / chaining via own short URLs | Reject long URLs whose host is this service | ProcessUrlController.EncodeAsync | EncodeUrl_SelfReferencingUrl_Returns400 |
| Store flooding / abuse | Fixed-window rate limit per client IP on encode (RateLimiting:EncodePermitLimitPerMinute, default 30) -> 429 | Startup + [EnableRateLimiting] | EncodeUrl_OverRateLimit_Returns429 |
| Oversized payloads | 8 KB request body limit on encode; URL max 2048 chars | [RequestSizeLimit], UrlValidationEngine | EncodeUrl_OversizedBody_IsRejected |
| Unsafe method use | Encode is POST only (GET returns 405) | Routing | EncodeUrl_GetMethod_Returns405 |
| Sniffing / clickjacking / referrer leakage | X-Content-Type-Options nosniff, X-Frame-Options DENY, Referrer-Policy no-referrer | Startup middleware | Responses_IncludeSecurityHeaders |
| Downgrade attacks | HTTPS redirection; HSTS outside Development | Startup | - |
| Data loss from tampered store | Corrupt file refused at startup, never overwritten | UrlMappingStore | Startup_CorruptStore_FailsAndLeavesFileUntouched |

## Known limits (accepted)
- No authentication on encode (public shortener by design).
- No malware / denylist reputation check on target URLs.
- Rate limiting is in-memory per instance; behind a proxy, configure ForwardedHeaders so the client IP is correct.
