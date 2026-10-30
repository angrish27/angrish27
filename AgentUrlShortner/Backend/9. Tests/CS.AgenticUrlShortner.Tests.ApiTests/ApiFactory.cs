using System.Collections.Generic;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.WebApiClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CS.AgenticUrlShortner.Tests.ApiTests
{
    internal sealed class ApiFactory : WebApplicationFactory<Startup>
    {
        private readonly string _storeFilePath;
        private readonly int? _encodePermitLimit;

        public ApiFactory(string storeFilePath, int? encodePermitLimit = null)
        {
            _storeFilePath = storeFilePath;
            _encodePermitLimit = encodePermitLimit;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { Constants.UrlStoreFilePath, _storeFilePath },
                    { Constants.EncodePermitLimitPerMinute, (_encodePermitLimit ?? 1000).ToString() }
                }));
        }
    }
}
