using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Accessors.Url;
using CS.AgenticUrlShortner.Common.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.AccessorTests
{
    [TestClass]
    public class UrlMappingStoreTests
    {
        private string _filePath;

        [TestInitialize]
        public void Initialize()
        {
            _filePath = Path.Combine(Path.GetTempPath(), $"url-store-{Guid.NewGuid():N}", "url-mappings.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        private static string Encode(long value) => $"code-{value}";

        private UrlMappingStore CreateLoadedStore()
        {
            var store = new UrlMappingStore();
            store.Load(_filePath);
            return store;
        }

        [TestMethod]
        public async Task UrlMappingStore_FirstMapping_UsesRequestEpochAndIsPersisted()
        {
            var store = CreateLoadedStore();
            const long requestEpochNanoseconds = 123456789;

            var (mapping, created) = await store.GetOrAddAsync("https://a.com", requestEpochNanoseconds, Encode, default);

            Assert.IsTrue(created);
            Assert.AreEqual(11157, mapping.SeqNumber);
            Assert.AreEqual("code-123456789", mapping.ShortCode);
            Assert.AreEqual(requestEpochNanoseconds, mapping.CodeEpochNanoseconds);

            using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
            var record = document.RootElement.EnumerateArray().Single();
            CollectionAssert.AreEqual(new[] { "seqNumber", "actualLongURL", "shortCode", "codeEpochNanoseconds" },
                record.EnumerateObject().Select(p => p.Name).ToArray());
        }

        [TestMethod]
        public async Task UrlMappingStore_Duplicate_ReturnsExistingWithoutNewId()
        {
            var store = CreateLoadedStore();
            await store.GetOrAddAsync("https://a.com", 100, Encode, default);

            var (mapping, created) = await store.GetOrAddAsync("https://a.com", 200, Encode, default);
            var (second, _) = await store.GetOrAddAsync("https://b.com", 100, Encode, default);

            Assert.IsFalse(created);
            Assert.AreEqual(11157, mapping.SeqNumber);
            Assert.AreEqual(11158, second.SeqNumber);
            Assert.AreEqual("code-101", second.ShortCode);
            Assert.AreEqual(101, second.CodeEpochNanoseconds);
        }

        [TestMethod]
        public async Task UrlMappingStore_ExactStringDeterminesDuplicates()
        {
            var store = CreateLoadedStore();
            await store.GetOrAddAsync("https://a.com", 100, Encode, default);

            var (_, created) = await store.GetOrAddAsync("https://A.com", 100, Encode, default);

            Assert.IsTrue(created);
        }

        [TestMethod]
        public async Task UrlMappingStore_Restart_LoadsMappingsAndContinuesSequence()
        {
            var first = CreateLoadedStore();
            await first.GetOrAddAsync("https://a.com", 100, Encode, default);

            var restarted = CreateLoadedStore();
            var (mapping, created) = await restarted.GetOrAddAsync("https://b.com", 100, Encode, default);

            Assert.AreEqual("https://a.com", restarted.FindByCode("code-100").ActualLongUrl);
            Assert.AreEqual("code-100", restarted.FindByLongUrl("https://a.com").ShortCode);
            Assert.IsTrue(created);
            Assert.AreEqual(11158, mapping.SeqNumber);
            Assert.AreEqual("code-101", mapping.ShortCode);
        }

        [TestMethod]
        public async Task UrlMappingStore_ConcurrentSameUrl_CreatesSingleMapping()
        {
            var store = CreateLoadedStore();

            var results = await Task.WhenAll(Enumerable.Range(0, 20)
                .Select(index => Task.Run(() => store.GetOrAddAsync($"https://a.com/{index}", 100, Encode, default))));

            Assert.AreEqual(20, results.Count(r => r.Created));
            Assert.AreEqual(20, results.Select(r => r.Mapping.ShortCode).Distinct().Count());
            Assert.AreEqual(20, results.Select(r => r.Mapping.CodeEpochNanoseconds).Distinct().Count());
        }

        [TestMethod]
        public async Task UrlMappingStore_LoadsLegacyBase62MappingsAndKeepsThemResolvable()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath,
                "[{\"seqNumber\":11157,\"actualLongURL\":\"https://a.com\",\"base62\":\"2tx\"}]");

            var store = CreateLoadedStore();
            var (newMapping, created) = await store.GetOrAddAsync("https://b.com", 100, Encode, default);

            Assert.AreEqual("https://a.com", store.FindByCode("2tx").ActualLongUrl);
            Assert.AreEqual("code-100", newMapping.ShortCode);
            Assert.IsTrue(created);
            using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
            Assert.AreEqual("2tx", document.RootElement[0].GetProperty("shortCode").GetString());
            Assert.AreEqual("code-100", document.RootElement[1].GetProperty("shortCode").GetString());
        }

        [TestMethod]
        [DataRow("{ not json")]
        [DataRow("")]
        [DataRow("null")]
        [DataRow("[{\"seqNumber\":11157,\"actualLongURL\":\"https://a.com\",\"base62\":\"2tx\"},{\"seqNumber\":11157,\"actualLongURL\":\"https://b.com\",\"base62\":\"2ty\"}]")]
        public void UrlMappingStore_CorruptFile_ThrowsAndLeavesFileUntouched(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath, content);

            var store = new UrlMappingStore();

            Assert.ThrowsException<AgenticUrlShortnerException>(() => store.Load(_filePath));
            Assert.AreEqual(content, File.ReadAllText(_filePath));
            Assert.ThrowsException<InvalidOperationException>(() => store.FindByCode("2tx"));
        }
    }
}
