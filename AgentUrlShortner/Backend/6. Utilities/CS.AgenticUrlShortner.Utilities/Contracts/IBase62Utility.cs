using CS.AgenticUrlShortner.Common.Contracts;

namespace CS.AgenticUrlShortner.Utilities.Contracts
{
    public interface IBase62Utility : IService
    {
        string Encode(long value);

        bool IsValid(string value);
    }
}
