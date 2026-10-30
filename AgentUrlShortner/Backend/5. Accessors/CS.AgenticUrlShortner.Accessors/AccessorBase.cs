using System.Runtime.CompilerServices;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Utilities;

[assembly: InternalsVisibleTo("CS.AgenticUrlShortner.Tests.AccessorTests")]

namespace CS.AgenticUrlShortner.Accessors
{
    internal abstract class AccessorBase : ServiceBase
    {
        internal UtilityFactory UtilityFactory { get; set; }

        protected AccessorBase()
        {
        }
    }
}
