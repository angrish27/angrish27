using System;
using System.IO;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common.Exceptions;
using CS.AgenticUrlShortner.Contracts.Client;
using CS.AgenticUrlShortner.Managers;
using CS.AgenticUrlShortner.Managers.Contracts;
using CS.AgenticUrlShortner.Shared.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.ManagerTests
{
    [TestClass]
    public class UrlManagerTests : TestBase
    {
        private string _directory;

        [TestInitialize]
        public void Initialize()
        {
            _directory = Path.Combine(Path.GetTempPath(), $"url-manager-{Guid.NewGuid():N}");
            ManagerFactory.Initialize(ServiceProvider, Path.Combine(_directory, "url-mappings.json"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [TestMethod]
        public async Task UrlManager_Encode_NewThenDuplicate()
        {
            var manager = ManagerFactory.Create<IUrlManager>();

            var first = await manager.EncodeUrlAsync(new EncodeUrlRequest { LongUrl = "https://a.com" });
            var duplicate = await manager.EncodeUrlAsync(new EncodeUrlRequest { LongUrl = "https://a.com" });
            var second = await manager.EncodeUrlAsync(new EncodeUrlRequest { LongUrl = "https://b.com" });

            Assert.IsTrue(first.IsNew);
            Assert.AreEqual("2tx", first.Code);
            Assert.IsFalse(duplicate.IsNew);
            Assert.AreEqual("2tx", duplicate.Code);
            Assert.AreEqual("2ty", second.Code);
        }

        [TestMethod]
        public async Task UrlManager_Encode_InvalidUrl_ThrowsBadRequest()
        {
            var manager = ManagerFactory.Create<IUrlManager>();

            await Assert.ThrowsExceptionAsync<BadRequestException>(() => manager.EncodeUrlAsync(new EncodeUrlRequest { LongUrl = "nope" }));
        }

        [TestMethod]
        public async Task UrlManager_Decode_ReturnsLongUrlOrNull()
        {
            var manager = ManagerFactory.Create<IUrlManager>();
            await manager.EncodeUrlAsync(new EncodeUrlRequest { LongUrl = "https://a.com/x" });

            Assert.AreEqual("https://a.com/x", (await manager.DecodeUrlAsync("2tx")).LongUrl);
            Assert.IsNull(await manager.DecodeUrlAsync("zzz"));
            Assert.IsNull(await manager.DecodeUrlAsync("bad-code"));
        }
    }
}
