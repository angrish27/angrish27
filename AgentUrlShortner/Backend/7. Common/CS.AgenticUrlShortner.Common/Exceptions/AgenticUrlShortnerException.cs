using System;
using System.Net;

namespace CS.AgenticUrlShortner.Common.Exceptions
{
    public sealed class AgenticUrlShortnerException : Exception
    {
        public HttpStatusCode StatusCode { get; } = HttpStatusCode.InternalServerError;

        public AgenticUrlShortnerException() { }

        public AgenticUrlShortnerException(string message) : base(message) { }

        public AgenticUrlShortnerException(string message, Exception innerException) : base(message, innerException) { }

        public AgenticUrlShortnerException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
