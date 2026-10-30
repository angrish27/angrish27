using System;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Utilities.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace CS.AgenticUrlShortner.Utilities
{
    public sealed class UtilityFactory : FactoryBase
    {
        public UtilityFactory(AmbientContext context, IServiceProvider serviceProvider)
            : base(context, serviceProvider)
        {
        }

        public static void RegisterTypes(IServiceCollection serviceCollection)
        {
            RegisterType<IIdentifierUtility, IdentifierUtility>(serviceCollection);
            RegisterType<IBase62Utility, Base62Utility>(serviceCollection);
        }

        public TService Create<TService>()
            where TService : class, IService
        {
            var utility = Resolve<TService>();

            if (utility is not UtilityBase utilityBase)
                throw new InvalidOperationException($"{typeof(TService).Name} does not implement UtilityBase.");

            utilityBase.Context = Context;
            utilityBase.Factory = this;

            return utility;
        }
    }
}
