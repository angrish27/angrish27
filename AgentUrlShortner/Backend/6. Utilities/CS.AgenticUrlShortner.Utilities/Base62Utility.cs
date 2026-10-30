using System;
using System.Text;
using CS.AgenticUrlShortner.Utilities.Contracts;

namespace CS.AgenticUrlShortner.Utilities
{
    internal sealed class Base62Utility : UtilityBase, IBase62Utility
    {
        internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public string Encode(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            if (value == 0)
                return Alphabet[0].ToString();

            var builder = new StringBuilder();
            while (value > 0)
            {
                builder.Insert(0, Alphabet[(int)(value % Alphabet.Length)]);
                value /= Alphabet.Length;
            }

            return builder.ToString();
        }

        public bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var c in value)
            {
                if (Alphabet.IndexOf(c, StringComparison.Ordinal) < 0)
                    return false;
            }

            return true;
        }
    }
}
