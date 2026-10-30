using System;
using System.Collections.Generic;

namespace CS.AgenticUrlShortner.Common.Exceptions
{
    public sealed class BadRequestException : Exception
    {
        public IReadOnlyDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

        public BadRequestException() { }

        public BadRequestException(string message) : base(message) { }

        public BadRequestException(string message, Exception innerException) : base(message, innerException) { }

        public BadRequestException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }
}
