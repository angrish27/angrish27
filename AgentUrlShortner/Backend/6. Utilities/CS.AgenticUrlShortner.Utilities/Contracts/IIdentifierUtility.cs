using CS.AgenticUrlShortner.Common.Contracts;

namespace CS.AgenticUrlShortner.Utilities.Contracts
{
    public interface IIdentifierUtility : IService
    {
        string NewId();
    }
}
