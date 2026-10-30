namespace CS.AgenticUrlShortner.Common
{
    public static class Constants
    {
        public const string ApplicationName = "CS.AgenticUrlShortner";
        public const string JsonContentType = "application/json";
        public const string CorrelationIdHeader = "x-correlation-id";
        public const string AllowedOrigins = "AllowedOrigins";
        public const string UrlStoreFilePath = "UrlStore:FilePath";
        public const string DefaultUrlStoreFilePath = "App_Data/url-mappings.json";
        public const string EncodeRateLimitPolicy = "encode";
        public const string EncodePermitLimitPerMinute = "RateLimiting:EncodePermitLimitPerMinute";
        public const int DefaultEncodePermitLimitPerMinute = 30;
        public const int MaxEncodeRequestBytes = 8 * 1024;
    }
}
