using CS.AgenticUrlShortner.Common.Contracts;

namespace CS.AgenticUrlShortner.Engines.Contracts
{
    public interface IUrlValidationEngine : IService
    {
        void ValidateLongUrl(string longUrl);
    }
}
