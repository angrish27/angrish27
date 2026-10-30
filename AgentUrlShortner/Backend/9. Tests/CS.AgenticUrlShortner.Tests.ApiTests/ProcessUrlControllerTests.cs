using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Exceptions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.ApiTests
{
    [TestClass]
    public class ProcessUrlControllerTests
    {
        private const string EncodeRoute = "/api/url/encode";
        private string _directory;
        private string _storeFilePath;

        [TestInitialize]
        public void Initialize()
        {
            _directory = Path.Combine(Path.GetTempPath(), $"api-tests-{Guid.NewGuid():N}");
            _storeFilePath = Path.Combine(_directory, "url-mappings.json");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private static HttpClient CreateClient(ApiFactory factory) =>
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        private static Task<HttpResponseMessage> EncodeAsync(HttpClient client, string longUrl) =>
            client.PostAsJsonAsync(EncodeRoute, new { longUrl });

        private static string GetCode(HttpResponseMessage response) =>
            response.Headers.Location.Segments.Last().TrimEnd('/');

        private static long DecodeEpochNanoseconds(string code)
        {
            var bytes = Convert.FromBase64String(code.Replace('-', '+').Replace('_', '/') + "=");
            return BinaryPrimitives.ReadInt64BigEndian(bytes);
        }

        [TestMethod]
        public async Task EncodeUrl_NewUrl_Returns201WithLocationAndOnlyShortUrl()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var before = (DateTimeOffset.UtcNow.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100;
            var response = await EncodeAsync(client, "https://example.com/a");
            var after = (DateTimeOffset.UtcNow.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100;
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            CollectionAssert.AreEqual(new[] { "shortUrl" }, json.RootElement.EnumerateObject().Select(p => p.Name).ToArray());
            var shortUrl = json.RootElement.GetProperty("shortUrl").GetString();
            var code = GetCode(response);
            Assert.AreEqual(shortUrl, response.Headers.Location?.ToString());
            Assert.AreEqual(11, code.Length);
            Assert.IsTrue(code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
            var codeEpochNanoseconds = DecodeEpochNanoseconds(code);
            Assert.IsTrue(codeEpochNanoseconds >= before && codeEpochNanoseconds <= after);
        }

        [TestMethod]
        public async Task EncodeUrl_DifferentUrls_ReturnUniqueEpochCodes()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var first = await EncodeAsync(client, "https://example.com/a");
            var second = await EncodeAsync(client, "https://example.com/b");

            var firstCode = GetCode(first);
            var secondCode = GetCode(second);
            Assert.AreNotEqual(firstCode, secondCode);
            Assert.IsTrue(DecodeEpochNanoseconds(secondCode) > DecodeEpochNanoseconds(firstCode));
        }

        [TestMethod]
        public async Task EncodeUrl_Duplicate_Returns200WithSameShortUrl()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var created = await EncodeAsync(client, "https://example.com/a");
            var duplicate = await EncodeAsync(client, "https://example.com/a");

            Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, duplicate.StatusCode);
            Assert.AreEqual(await created.Content.ReadAsStringAsync(), await duplicate.Content.ReadAsStringAsync());
        }

        [TestMethod]
        [DataRow("not a url")]
        [DataRow("ftp://example.com")]
        [DataRow("/relative")]
        [DataRow("")]
        public async Task EncodeUrl_InvalidUrl_Returns400AndDoesNotPersist(string longUrl)
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await EncodeAsync(client, longUrl);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsFalse(File.Exists(_storeFilePath));
        }

        [TestMethod]
        public async Task EncodeUrl_MissingBody_Returns400()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await client.PostAsync(EncodeRoute, new StringContent("", Encoding.UTF8, "application/json"));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task DecodeUrl_KnownCode_Returns302WithLongUrlLocation()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var encodeResponse = await EncodeAsync(client, "https://example.com/target?x=1");
            var response = await client.GetAsync($"/{GetCode(encodeResponse)}");

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("https://example.com/target?x=1", response.Headers.Location?.ToString());
        }

        [TestMethod]
        public async Task DecodeUrl_LegacyBase62Code_RemainsAvailable()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_storeFilePath,
                "[{\"seqNumber\":11157,\"actualLongURL\":\"https://example.com/legacy\",\"base62\":\"2tx\"}]");
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await client.GetAsync("/2tx");

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual("https://example.com/legacy", response.Headers.Location?.ToString());
        }

        [TestMethod]
        [DataRow("/zzzz")]
        [DataRow("/bad-code")]
        public async Task DecodeUrl_UnknownCode_Returns404(string path)
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await client.GetAsync(path);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Restart_LoadsPersistedMappingsAndContinuesEpochCodes()
        {
            string firstCode;
            using (var firstRun = new ApiFactory(_storeFilePath))
            using (var client = CreateClient(firstRun))
            {
                var firstResponse = await EncodeAsync(client, "https://example.com/a");
                firstCode = GetCode(firstResponse);
            }

            using var secondRun = new ApiFactory(_storeFilePath);
            using var restartedClient = CreateClient(secondRun);

            var redirect = await restartedClient.GetAsync($"/{firstCode}");
            var duplicate = await EncodeAsync(restartedClient, "https://example.com/a");
            var next = await EncodeAsync(restartedClient, "https://example.com/b");

            Assert.AreEqual(HttpStatusCode.Redirect, redirect.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, duplicate.StatusCode);
            Assert.AreEqual(HttpStatusCode.Created, next.StatusCode);
            Assert.IsTrue(DecodeEpochNanoseconds(GetCode(next)) > DecodeEpochNanoseconds(firstCode));
        }

        [TestMethod]
        public void Startup_CorruptStore_FailsAndLeavesFileUntouched()
        {
            Directory.CreateDirectory(_directory);
            const string corrupt = "{ this is not json";
            File.WriteAllText(_storeFilePath, corrupt);

            using var factory = new ApiFactory(_storeFilePath);

            var exception = Assert.ThrowsException<AgenticUrlShortnerException>(() => factory.CreateClient());
            StringAssert.Contains(exception.Message, "corrupt");
            Assert.AreEqual(corrupt, File.ReadAllText(_storeFilePath));
        }

        [TestMethod]
        public async Task EncodeUrl_SelfReferencingUrl_Returns400()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await EncodeAsync(client, $"https://{client.BaseAddress.Host}/2tx");

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsFalse(File.Exists(_storeFilePath));
        }

        [TestMethod]
        public async Task EncodeUrl_OverRateLimit_Returns429()
        {
            using var factory = new ApiFactory(_storeFilePath, encodePermitLimit: 2);
            using var client = CreateClient(factory);

            await EncodeAsync(client, "https://example.com/1");
            await EncodeAsync(client, "https://example.com/2");
            var third = await EncodeAsync(client, "https://example.com/3");

            Assert.AreEqual(HttpStatusCode.TooManyRequests, third.StatusCode);
        }

        [TestMethod]
        public async Task EncodeUrl_OversizedBody_IsRejected()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);
            var body = "{\"longUrl\":\"https://example.com/" + new string('a', 20_000) + "\"}";

            var response = await client.PostAsync(EncodeRoute, new StringContent(body, Encoding.UTF8, "application/json"));

            Assert.IsTrue(response.StatusCode is HttpStatusCode.RequestEntityTooLarge or HttpStatusCode.BadRequest, response.StatusCode.ToString());
            Assert.IsFalse(File.Exists(_storeFilePath));
        }

        [TestMethod]
        public async Task EncodeUrl_GetMethod_Returns405()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await client.GetAsync(EncodeRoute);

            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [TestMethod]
        public async Task Responses_IncludeSecurityHeaders()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);

            var response = await client.GetAsync("/health");

            Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.AreEqual("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
            Assert.AreEqual("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        }

        [TestMethod]
        public async Task ExistingPipeline_HealthAndCorrelationHeader_StillWork()
        {
            using var factory = new ApiFactory(_storeFilePath);
            using var client = CreateClient(factory);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add(Constants.CorrelationIdHeader, "brownfield-check");

            var response = await client.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("brownfield-check", response.Headers.GetValues(Constants.CorrelationIdHeader).Single());
        }
    }
}
