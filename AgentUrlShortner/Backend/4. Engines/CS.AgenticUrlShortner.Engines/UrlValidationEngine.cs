using System;
using System.Collections.Generic;
using CS.AgenticUrlShortner.Common.Exceptions;
using CS.AgenticUrlShortner.Engines.Contracts;

namespace CS.AgenticUrlShortner.Engines
{
    internal sealed class UrlValidationEngine : EngineBase, IUrlValidationEngine
    {
        internal const int MaxUrlLength = 2048;
        private const string LongUrlField = "LongUrl";

        public void ValidateLongUrl(string longUrl)
        {
            var error = GetError(longUrl);
            if (error == null)
                return;

            throw new BadRequestException("Validation failed.", new Dictionary<string, string[]>
            {
                { LongUrlField, [error] }
            });
        }

        private static string GetError(string longUrl)
        {
            if (string.IsNullOrWhiteSpace(longUrl))
                return "LongUrl is required.";

            if (longUrl.Length > MaxUrlLength)
                return $"LongUrl must not exceed {MaxUrlLength} characters.";

            if (!string.Equals(longUrl, longUrl.Trim(), StringComparison.Ordinal))
                return "LongUrl must not contain leading or trailing whitespace.";

            foreach (var c in longUrl)
            {
                if (char.IsControl(c))
                    return "LongUrl must not contain control characters.";
            }

            if (!Uri.TryCreate(longUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrEmpty(uri.Host))
                return "LongUrl must be an absolute http or https URL.";

            if (!string.IsNullOrEmpty(uri.UserInfo))
                return "LongUrl must not contain embedded credentials.";

            return null;
        }
    }
}
