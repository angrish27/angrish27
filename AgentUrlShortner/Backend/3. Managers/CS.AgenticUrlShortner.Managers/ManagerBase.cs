using System.Runtime.CompilerServices;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Engines;
using CS.AgenticUrlShortner.Utilities;

[assembly: InternalsVisibleTo("CS.AgenticUrlShortner.Tests.ManagerTests")]

namespace CS.AgenticUrlShortner.Managers
{
    internal abstract class ManagerBase : ServiceBase
    {
        internal EngineFactory EngineFactory { get; set; }

        internal AccessorFactory AccessorFactory { get; set; }

        internal UtilityFactory UtilityFactory { get; set; }

        protected ManagerBase()
        {
        }
    }
}
