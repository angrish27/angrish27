using System;
using CS.AgenticUrlShortner.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CS.AgenticUrlShortner.Common
{
    public abstract class FactoryBase
    {
        public AmbientContext Context { get; }

        protected IServiceProvider ServiceProvider { get; }

        protected FactoryBase(AmbientContext context, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Context = context ?? new AmbientContext();
        }

        public static void RegisterType<TInterface, TImplementation>(IServiceCollection serviceCollection)
            where TInterface : IService
            where TImplementation : IService, new()
        {
            RegisterType(serviceCollection, typeof(TInterface), typeof(TImplementation));
        }

        protected static void RegisterType(IServiceCollection serviceCollection, Type contract, Type implementation)
        {
            ArgumentNullException.ThrowIfNull(serviceCollection);
            ValidateRegisterTypeArguments(contract, implementation);
            serviceCollection.TryAddTransient(contract, implementation);
        }

        private static void ValidateRegisterTypeArguments(Type contract, Type implementation)
        {
            if (contract == null)
                throw new InvalidOperationException($"The supplied '{nameof(contract)}' type is null and must be specified.");

            if (implementation == null)
                throw new InvalidOperationException($"The supplied '{nameof(implementation)}' type is null and must be specified.");

            if (!typeof(IService).IsAssignableFrom(contract))
                throw new InvalidOperationException($"Type contract '{contract.FullName}' must implement '{typeof(IService).FullName}'");

            if (!contract.IsAssignableFrom(implementation))
                throw new InvalidOperationException($"The type '{implementation.FullName}' must implement '{contract.FullName}'");

            if (implementation.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Type '{implementation.FullName}' cannot be used because it does not have a parameterless constructor.");
        }

        protected TService Resolve<TService>() where TService : class, IService
        {
            var instance = ServiceProvider.GetService(typeof(TService));

            if (instance == null)
                throw new InvalidOperationException($"The type '{typeof(TService).Name}' has not been registered in this factory.");

            return (TService)instance;
        }

        public TService ResolveRequiredService<TService>() where TService : class
        {
            return ServiceProvider.GetRequiredService<TService>();
        }
    }
}
