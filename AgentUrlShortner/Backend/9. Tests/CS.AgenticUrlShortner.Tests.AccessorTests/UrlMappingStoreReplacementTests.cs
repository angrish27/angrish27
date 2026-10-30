using System;
using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Accessors.Contracts;
using CS.AgenticUrlShortner.Accessors.Url;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Contracts.Logic;
using CS.AgenticUrlShortner.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.AccessorTests
{
    [TestClass]
    public class UrlMappingStoreReplacementTests
    {
        [TestMethod]
        public void UrlMappingStore_DefaultRegistration_IsJsonFileStore()
        {
            var services = new ServiceCollection();
            AccessorFactory.RegisterTypes(services);

            using var provider = services.BuildServiceProvider();

            Assert.IsInstanceOfType<UrlMappingStore>(provider.GetRequiredService<IUrlMappingStore>());
        }

        [TestMethod]
        public async Task UrlMappingAccessor_UsesReplacementStore_WhenRegisteredFirst()
        {
            var fake = new FakeStore();
            var services = new ServiceCollection();
            services.AddSingleton<IUrlMappingStore>(fake);
            UtilityFactory.RegisterTypes(services);
            AccessorFactory.RegisterTypes(services);
            using var provider = services.BuildServiceProvider();
            var accessor = new AccessorFactory(new AmbientContext(), provider).Create<IUrlMappingAccessor>();

            var (mapping, created) = await accessor.GetOrCreateAsync("https://example.com", 1);

            Assert.IsTrue(created);
            Assert.AreSame(fake.Mapping, mapping);
            Assert.AreSame(fake.Mapping, accessor.FindByCode("fake"));
        }

        private sealed class FakeStore : IUrlMappingStore
        {
            public UrlMapping Mapping { get; } = new() { SeqNumber = 1, ActualLongUrl = "https://example.com", ShortCode = "fake" };

            public void Load(string location) { }

            public UrlMapping FindByLongUrl(string longUrl) => null;

            public UrlMapping FindByCode(string code) => code == Mapping.ShortCode ? Mapping : null;

            public Task<(UrlMapping Mapping, bool Created)> GetOrAddAsync(string longUrl, long requestEpochNanoseconds,
                Func<long, string> encode, CancellationToken cancellationToken) =>
                Task.FromResult((Mapping, true));
        }
    }
}
