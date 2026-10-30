using System;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Engines.Contracts;
using CS.AgenticUrlShortner.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CS.AgenticUrlShortner.Engines
{
    public sealed class EngineFactory : FactoryBase
    {
        public EngineFactory(AmbientContext context, IServiceProvider serviceProvider)
            : base(context, serviceProvider)
        {
        }

        public static void RegisterTypes(IServiceCollection serviceCollection)
        {
            RegisterType<IUrlValidationEngine, UrlValidationEngine>(serviceCollection);
        }

        public TService Create<TService>()
            where TService : class, IService
        {
            return Create<TService>(null, null);
        }

        public TService Create<TService>(AccessorFactory accessorFactory, UtilityFactory utilityFactory)
            where TService : class, IService
        {
            var engine = Resolve<TService>();

            if (engine is not EngineBase engineBase)
                throw new InvalidOperationException($"{typeof(TService).Name} does not implement EngineBase.");

            engineBase.Context = Context;
            engineBase.Factory = this;
            engineBase.AccessorFactory = accessorFactory ?? new AccessorFactory(Context, ServiceProvider);
            engineBase.UtilityFactory = utilityFactory ?? new UtilityFactory(Context, ServiceProvider);

            return engine;
        }
    }
}
