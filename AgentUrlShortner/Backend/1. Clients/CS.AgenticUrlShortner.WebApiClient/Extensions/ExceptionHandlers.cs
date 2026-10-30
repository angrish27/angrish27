using System;
using System.Net;
using CS.AgenticUrlShortner.Common.Exceptions;

namespace CS.AgenticUrlShortner.WebApiClient.Extensions
{
    internal sealed class BadRequestExceptionHandler : IExceptionHandler
    {
        public (HttpStatusCode StatusCode, object Body) HandleException(Exception exception)
        {
            var badRequest = (BadRequestException)exception;
            return (HttpStatusCode.BadRequest, new { error = badRequest.Message, errors = badRequest.Errors });
        }
    }

    internal sealed class AgenticUrlShortnerExceptionHandler : IExceptionHandler
    {
        public (HttpStatusCode StatusCode, object Body) HandleException(Exception exception)
        {
            var appException = (AgenticUrlShortnerException)exception;
            return (appException.StatusCode, new { error = appException.Message });
        }
    }
}
