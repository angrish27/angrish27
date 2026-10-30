using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.RateLimiting;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Managers;
using CS.AgenticUrlShortner.WebApiClient.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CS.AgenticUrlShortner.WebApiClient
{
    public class Startup
    {
        private const string AllowedSpecificOrigins = "_allowedSpecificOrigins";
        private readonly AsyncLocal<AmbientContext> _ambientContext = new();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            ManagerFactory.RegisterTypes(services);
            services.AddTransient(serviceProvider => new ManagerFactory(_ambientContext.Value, serviceProvider));

            var allowedOrigins = Configuration[Constants.AllowedOrigins]?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddCors(options =>
            {
                options.AddPolicy(AllowedSpecificOrigins, builder =>
                {
                    if (allowedOrigins is { Length: > 0 })
                        builder.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
                });
            });

            var encodePermitLimit = Configuration.GetValue(Constants.EncodePermitLimitPerMinute, Constants.DefaultEncodePermitLimitPerMinute);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(Constants.EncodeRateLimitPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = encodePermitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            services.AddHealthChecks();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var urlStoreFilePath = Path.Combine(env.ContentRootPath,
                Configuration[Constants.UrlStoreFilePath] ?? Constants.DefaultUrlStoreFilePath);
            ManagerFactory.Initialize(app.ApplicationServices, urlStoreFilePath);

            app.AddExceptionHandler(loggerFactory, env.IsDevelopment());

            if (!env.IsDevelopment())
                app.UseHsts();

            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
                context.Response.Headers["X-Frame-Options"] = "DENY";
                await next(context).ConfigureAwait(false);
            });

            app.UseRouting();
            app.UseCors(AllowedSpecificOrigins);
            app.UseRateLimiter();

            app.Use(async (context, next) =>
            {
                var correlationId = context.Request.Headers[Constants.CorrelationIdHeader].FirstOrDefault()
                    ?? context.TraceIdentifier;

                _ambientContext.Value = new AmbientContext
                {
                    CorrelationId = correlationId,
                    UserId = context.User?.Identity?.Name,
                    Claims = context.User?.Claims.ToArray()
                };

                context.Response.Headers[Constants.CorrelationIdHeader] = correlationId;
                await next(context).ConfigureAwait(false);
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }
    }
}
