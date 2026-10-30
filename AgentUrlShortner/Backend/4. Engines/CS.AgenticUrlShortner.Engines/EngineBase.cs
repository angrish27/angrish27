using System.Runtime.CompilerServices;
using CS.AgenticUrlShortner.Accessors;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Utilities;

[assembly: InternalsVisibleTo("CS.AgenticUrlShortner.Tests.EngineTests")]

namespace CS.AgenticUrlShortner.Engines
{
    internal abstract class EngineBase : ServiceBase
    {
        internal AccessorFactory AccessorFactory { get; set; }

        internal UtilityFactory UtilityFactory { get; set; }

        protected EngineBase()
        {
        }
    }
}
