using System;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Shared.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CS.AgenticUrlShortner.Tests.CommonTests
{
    [TestClass]
    public class FactoryBaseTests : TestBase
    {
        private interface IUnregisteredService : IService { }

        private interface IInvalidService : IService { }

        private sealed class NoDefaultCtorService : ServiceBase, IInvalidService
        {
            public NoDefaultCtorService(string value) => _ = value;
        }

        private sealed class TestFactory : FactoryBase
        {
            public TestFactory(AmbientContext context, IServiceProvider serviceProvider) : base(context, serviceProvider) { }

            public TService Get<TService>() where TService : class, IService => Resolve<TService>();

            public static void Register(IServiceCollection services, Type contract, Type implementation) =>
                RegisterType(services, contract, implementation);
        }

        [TestMethod]
        public void FactoryBase_ResolveUnregistered_Throws()
        {
            var factory = new TestFactory(Context, ServiceProvider);

            Assert.ThrowsException<InvalidOperationException>(() => factory.Get<IUnregisteredService>());
        }

        [TestMethod]
        public void FactoryBase_RegisterWithoutParameterlessCtor_Throws()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                TestFactory.Register(new ServiceCollection(), typeof(IInvalidService), typeof(NoDefaultCtorService)));
        }
    }
}
