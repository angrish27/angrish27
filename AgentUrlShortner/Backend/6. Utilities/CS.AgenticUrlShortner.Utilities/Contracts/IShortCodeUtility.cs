using CS.AgenticUrlShortner.Common.Contracts;

namespace CS.AgenticUrlShortner.Utilities.Contracts
{
    public interface IShortCodeUtility : IService
    {
        long ToUnixEpochNanoseconds(System.DateTimeOffset timestampUtc);

        string EncodeUnixEpochNanoseconds(long unixEpochNanoseconds);

        bool TryDecodeUnixEpochNanoseconds(string code, out long unixEpochNanoseconds);

        bool IsValid(string code);
    }
}
