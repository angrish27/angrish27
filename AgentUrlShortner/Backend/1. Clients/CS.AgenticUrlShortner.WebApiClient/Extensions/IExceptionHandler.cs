using System;
using System.Net;

namespace CS.AgenticUrlShortner.WebApiClient.Extensions
{
    internal interface IExceptionHandler
    {
        (HttpStatusCode StatusCode, object Body) HandleException(Exception exception);
    }
}
