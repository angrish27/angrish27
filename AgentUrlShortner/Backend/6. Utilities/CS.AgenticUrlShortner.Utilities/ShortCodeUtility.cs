using System;
using System.Buffers.Binary;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Utilities.Contracts;

namespace CS.AgenticUrlShortner.Utilities
{
    internal sealed class ShortCodeUtility : UtilityBase, IShortCodeUtility
    {
        public long ToUnixEpochNanoseconds(DateTimeOffset timestampUtc)
        {
            var ticksSinceEpoch = timestampUtc.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks;
            return checked(ticksSinceEpoch * 100);
        }

        public string EncodeUnixEpochNanoseconds(long unixEpochNanoseconds)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(unixEpochNanoseconds);

            Span<byte> bytes = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64BigEndian(bytes, unixEpochNanoseconds);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public bool TryDecodeUnixEpochNanoseconds(string code, out long unixEpochNanoseconds)
        {
            unixEpochNanoseconds = 0;
            if (code == null || code.Length != 11)
                return false;

            foreach (var character in code)
            {
                if (Constants.Base64UrlAlphabet.IndexOf(character) < 0)
                    return false;
            }

            var base64 = code.Replace('-', '+').Replace('_', '/') + "=";
            Span<byte> bytes = stackalloc byte[sizeof(long)];
            if (!Convert.TryFromBase64String(base64, bytes, out var bytesWritten)
                || bytesWritten != sizeof(long))
                return false;

            var value = BinaryPrimitives.ReadInt64BigEndian(bytes);
            if (value < 0 || !string.Equals(EncodeUnixEpochNanoseconds(value), code, StringComparison.Ordinal))
                return false;

            unixEpochNanoseconds = value;
            return true;
        }

        public bool IsValid(string code)
        {
            if (TryDecodeUnixEpochNanoseconds(code, out _))
                return true;

            if (string.IsNullOrEmpty(code))
                return false;

            foreach (var character in code)
            {
                if (Constants.LegacyBase62Alphabet.IndexOf(character) < 0)
                    return false;
            }

            return true;
        }
    }
}
