using System;
using CS.AgenticUrlShortner.Accessors.Contracts;
using CS.AgenticUrlShortner.Accessors.Url;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CS.AgenticUrlShortner.Accessors
{
    public sealed class AccessorFactory : FactoryBase
    {
        public AccessorFactory(AmbientContext context, IServiceProvider serviceProvider)
            : base(context, serviceProvider)
        {
        }

        public static void RegisterTypes(IServiceCollection serviceCollection)
        {
            RegisterType<IUrlMappingAccessor, UrlMappingAccessor>(serviceCollection);

            serviceCollection.TryAddSingleton<IUrlMappingStore, UrlMappingStore>();
        }

        public static void Initialize(IServiceProvider serviceProvider, string urlStoreFilePath)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            serviceProvider.GetRequiredService<IUrlMappingStore>().Load(urlStoreFilePath);
        }

        public TService Create<TService>()
            where TService : class, IService
        {
            return Create<TService>(null);
        }

        public TService Create<TService>(UtilityFactory utilityFactory)
            where TService : class, IService
        {
            var accessor = Resolve<TService>();

            if (accessor is not AccessorBase accessorBase)
                throw new InvalidOperationException($"{typeof(TService).Name} does not implement AccessorBase.");

            accessorBase.Context = Context;
            accessorBase.Factory = this;
            accessorBase.UtilityFactory = utilityFactory ?? new UtilityFactory(Context, ServiceProvider);

            return accessor;
        }
    }
}
