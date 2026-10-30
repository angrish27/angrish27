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

        private static string Encode(long value) => value switch { 11157 => "2tx", 11158 => "2ty", _ => value.ToString() };

        private UrlMappingStore CreateLoadedStore()
        {
            var store = new UrlMappingStore();
            store.Load(_filePath);
            return store;
        }

        [TestMethod]
        public async Task UrlMappingStore_FirstMapping_Gets11157AndIsPersisted()
        {
            var store = CreateLoadedStore();

            var (mapping, created) = await store.GetOrAddAsync("https://a.com", Encode, default);

            Assert.IsTrue(created);
            Assert.AreEqual(11157, mapping.SeqNumber);
            Assert.AreEqual("2tx", mapping.Base62);

            using var document = JsonDocument.Parse(File.ReadAllText(_filePath));
            var record = document.RootElement.EnumerateArray().Single();
            CollectionAssert.AreEqual(new[] { "seqNumber", "actualLongURL", "base62" },
                record.EnumerateObject().Select(p => p.Name).ToArray());
        }

        [TestMethod]
        public async Task UrlMappingStore_Duplicate_ReturnsExistingWithoutNewId()
        {
            var store = CreateLoadedStore();
            await store.GetOrAddAsync("https://a.com", Encode, default);

            var (mapping, created) = await store.GetOrAddAsync("https://a.com", Encode, default);
            var (second, _) = await store.GetOrAddAsync("https://b.com", Encode, default);

            Assert.IsFalse(created);
            Assert.AreEqual(11157, mapping.SeqNumber);
            Assert.AreEqual(11158, second.SeqNumber);
            Assert.AreEqual("2ty", second.Base62);
        }

        [TestMethod]
        public async Task UrlMappingStore_ExactStringDeterminesDuplicates()
        {
            var store = CreateLoadedStore();
            await store.GetOrAddAsync("https://a.com", Encode, default);

            var (_, created) = await store.GetOrAddAsync("https://A.com", Encode, default);

            Assert.IsTrue(created);
        }

        [TestMethod]
        public async Task UrlMappingStore_Restart_LoadsMappingsAndContinuesSequence()
        {
            var first = CreateLoadedStore();
            await first.GetOrAddAsync("https://a.com", Encode, default);

            var restarted = CreateLoadedStore();
            var (mapping, created) = await restarted.GetOrAddAsync("https://b.com", Encode, default);

            Assert.AreEqual("https://a.com", restarted.FindByCode("2tx").ActualLongUrl);
            Assert.AreEqual("2tx", restarted.FindByLongUrl("https://a.com").Base62);
            Assert.IsTrue(created);
            Assert.AreEqual(11158, mapping.SeqNumber);
        }

        [TestMethod]
        public async Task UrlMappingStore_ConcurrentSameUrl_CreatesSingleMapping()
        {
            var store = CreateLoadedStore();

            var results = await Task.WhenAll(Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => store.GetOrAddAsync("https://a.com", Encode, default))));

            Assert.AreEqual(1, results.Count(r => r.Created));
            Assert.IsTrue(results.All(r => r.Mapping.SeqNumber == 11157));
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
