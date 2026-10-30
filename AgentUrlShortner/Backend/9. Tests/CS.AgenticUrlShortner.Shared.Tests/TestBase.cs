using System;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Engines;
using CS.AgenticUrlShortner.Managers;
using CS.AgenticUrlShortner.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CS.AgenticUrlShortner.Shared.Tests
{
    public abstract class TestBase
    {
        protected AmbientContext Context { get; } = new() { CorrelationId = Guid.NewGuid().ToString("N"), UserId = "unit-test" };

        protected IServiceProvider ServiceProvider { get; }

        protected TestBase()
        {
            var services = new ServiceCollection();
            ManagerFactory.RegisterTypes(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        protected ManagerFactory ManagerFactory => new(Context, ServiceProvider);

        protected EngineFactory EngineFactory => new(Context, ServiceProvider);

        protected AccessorFactory AccessorFactory => new(Context, ServiceProvider);

        protected UtilityFactory UtilityFactory => new(Context, ServiceProvider);
    }
}
