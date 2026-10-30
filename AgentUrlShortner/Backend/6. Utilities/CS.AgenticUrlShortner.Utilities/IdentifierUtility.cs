using System;
using CS.AgenticUrlShortner.Utilities.Contracts;

namespace CS.AgenticUrlShortner.Utilities
{
    internal sealed class IdentifierUtility : UtilityBase, IIdentifierUtility
    {
        public string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
