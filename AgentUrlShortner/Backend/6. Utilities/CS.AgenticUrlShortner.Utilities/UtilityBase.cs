using System.Runtime.CompilerServices;
using CS.AgenticUrlShortner.Common;

[assembly: InternalsVisibleTo("CS.AgenticUrlShortner.Tests.UtilityTests")]

namespace CS.AgenticUrlShortner.Utilities
{
    internal abstract class UtilityBase : ServiceBase
    {
        protected UtilityBase()
        {
        }
    }
}
