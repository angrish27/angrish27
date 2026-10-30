using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CS.AgenticUrlShortner.WebApiClient.Extensions
{
    public static class ExceptionHandler
    {
        private static readonly Dictionary<Type, IExceptionHandler> Strategy = new()
        {
            { typeof(BadRequestException), new BadRequestExceptionHandler() },
            { typeof(AgenticUrlShortnerException), new AgenticUrlShortnerExceptionHandler() }
        };

        public static void AddExceptionHandler(this IApplicationBuilder app, ILoggerFactory loggerFactory, bool includeDetails)
        {
            app.UseExceptionHandler(appBuilder =>
                appBuilder.Run(context => OnHandleError(context, loggerFactory, includeDetails)));
        }

        private static async Task OnHandleError(HttpContext context, ILoggerFactory loggerFactory, bool includeDetails)
        {
            var logger = loggerFactory.CreateLogger(Constants.ApplicationName);
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            if (error != null && Strategy.TryGetValue(error.GetType(), out var handler))
            {
                var (statusCode, body) = handler.HandleException(error);
                logger.LogWarning(error, "Handled exception: {Message}", error.Message);
                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsJsonAsync(body).ConfigureAwait(false);
                return;
            }

            logger.LogError(error, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = includeDetails ? error?.ToString() : "An unexpected error has occurred."
            }).ConfigureAwait(false);
        }
    }
}
