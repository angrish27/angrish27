using CS.AgenticUrlShortner.Common.Exceptions;
using CS.AgenticUrlShortner.Engines.Contracts;
using CS.AgenticUrlShortner.Shared.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.EngineTests
{
    [TestClass]
    public class UrlValidationEngineTests : TestBase
    {
        [TestMethod]
        [DataRow("https://example.com/a?b=1")]
        [DataRow("http://example.com")]
        public void UrlValidationEngine_ValidUrl_DoesNotThrow(string url)
        {
            EngineFactory.Create<IUrlValidationEngine>().ValidateLongUrl(url);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("not a url")]
        [DataRow("/relative/path")]
        [DataRow("ftp://example.com")]
        [DataRow(" https://example.com")]
        [DataRow("https://user:pass@example.com")]
        [DataRow("https://google.com@evil.example")]
        [DataRow("https://example.com/a\tb")]
        public void UrlValidationEngine_InvalidUrl_ThrowsBadRequest(string url)
        {
            Assert.ThrowsException<BadRequestException>(() => EngineFactory.Create<IUrlValidationEngine>().ValidateLongUrl(url));
        }
    }
}
