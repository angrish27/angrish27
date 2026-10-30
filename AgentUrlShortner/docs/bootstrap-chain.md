# Bootstrap Chain

## 1. Host startup
`1. Clients/CS.AgenticUrlShortner.WebApiClient/Program.cs`
```
Program.Main → Host.CreateDefaultBuilder(args)
			 → ConfigureWebHostDefaults(UseStartup<Startup>)
			 → Build().Run()
```

## 2. Service registration (`Startup.ConfigureServices`)
```
ManagerFactory.RegisterTypes(services)
 ├─ RegisterType<IUrlManager, UrlManager>
 ├─ EngineFactory.RegisterTypes    → RegisterType<IUrlValidationEngine, UrlValidationEngine>
 ├─ AccessorFactory.RegisterTypes  → RegisterType<IUrlMappingAccessor, UrlMappingAccessor>, TryAddSingleton<IUrlMappingStore, UrlMappingStore>
 └─ UtilityFactory.RegisterTypes   → RegisterType<IIdentifierUtility, IdentifierUtility>, RegisterType<IBase62Utility, Base62Utility>
services.AddTransient(sp => new ManagerFactory(_ambientContext.Value, sp))
services.AddCors(...AllowedOrigins...)
services.AddHealthChecks()
services.AddControllers()
```
Every `RegisterType` → `FactoryBase.RegisterType` → validates contract/impl → `TryAddTransient`.

**Adding a new service = add one `RegisterType<IFoo, Foo>` line in the owning layer's factory `RegisterTypes`.**
Nothing is registered directly in `Startup` except `ManagerFactory`.

## 3. Request pipeline (`Startup.Configure`)
```
AddExceptionHandler (UseExceptionHandler + strategy map)
UseHttpsRedirection
UseRouting
UseCors(_allowedSpecificOrigins)
Correlation middleware → sets AsyncLocal<AmbientContext> { CorrelationId, UserId, Claims }
					   → echoes x-correlation-id response header
UseEndpoints → /health, controllers
```

## 4. Per-request creation chain
```
Controller (ctor: ManagerFactory from DI, carries AmbientContext)
 └─ ManagerFactory.Create<IXManager>(engineFactory?, accessorFactory?, utilityFactory?)
	 ├─ Resolve<IXManager>()                       // transient from DI
	 ├─ sets Context, Factory
	 ├─ EngineFactory   = arg ?? new EngineFactory(Context, sp)
	 ├─ AccessorFactory = arg ?? new AccessorFactory(Context, sp)
	 └─ UtilityFactory  = arg ?? new UtilityFactory(Context, sp)
		 Manager → EngineFactory.Create<IXEngine>(AccessorFactory, UtilityFactory)
		 Manager → AccessorFactory.Create<IXAccessor>(UtilityFactory)
		 Any     → UtilityFactory.Create<IXUtility>()
```
The same `AmbientContext` instance flows down the whole chain. Passing existing factories down
(`Create<T>(AccessorFactory, UtilityFactory)`) keeps a single factory graph per request and allows
tests to substitute fakes.

## 5. Checklist when adding a component
1. Interface in `<Layer>/Contracts/I<Name>.cs` inheriting `IService`.
2. `internal sealed class <Name> : <Layer>Base, I<Name>` with a parameterless ctor.
3. `RegisterType<I<Name>, <Name>>(serviceCollection);` in `<Layer>Factory.RegisterTypes`.
4. Consume via the factory, never `new` and never constructor injection.
5. Add tests in the matching `9. Tests` project.
