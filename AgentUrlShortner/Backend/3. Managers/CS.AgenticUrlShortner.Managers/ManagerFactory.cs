using System;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Engines;
using CS.AgenticUrlShortner.Managers.Contracts;
using CS.AgenticUrlShortner.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace CS.AgenticUrlShortner.Managers
{
    public sealed class ManagerFactory : FactoryBase
    {
        public ManagerFactory(AmbientContext context, IServiceProvider serviceProvider)
            : base(context, serviceProvider)
        {
        }

        public static void RegisterTypes(IServiceCollection serviceCollection)
        {
            RegisterType<IUrlManager, UrlManager>(serviceCollection);

            EngineFactory.RegisterTypes(serviceCollection);
            AccessorFactory.RegisterTypes(serviceCollection);
            UtilityFactory.RegisterTypes(serviceCollection);
        }

        public static void Initialize(IServiceProvider serviceProvider, string urlStoreFilePath)
        {
            AccessorFactory.Initialize(serviceProvider, urlStoreFilePath);
        }

        public TService Create<TService>()
            where TService : class, IService
        {
            return Create<TService>(null, null, null);
        }

        public TService Create<TService>(EngineFactory engineFactory, AccessorFactory accessorFactory,
            UtilityFactory utilityFactory)
            where TService : class, IService
        {
            var manager = Resolve<TService>();

            if (manager is not ManagerBase managerBase)
                throw new InvalidOperationException($"{typeof(TService).Name} does not implement ManagerBase.");

            managerBase.Context = Context;
            managerBase.Factory = this;
            managerBase.EngineFactory = engineFactory ?? new EngineFactory(Context, ServiceProvider);
            managerBase.AccessorFactory = accessorFactory ?? new AccessorFactory(Context, ServiceProvider);
            managerBase.UtilityFactory = utilityFactory ?? new UtilityFactory(Context, ServiceProvider);

            return manager;
        }
    }
}
