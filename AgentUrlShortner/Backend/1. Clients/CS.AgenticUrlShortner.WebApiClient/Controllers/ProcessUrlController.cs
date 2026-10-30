using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common;
using CS.AgenticUrlShortner.Common.Exceptions;
using CS.AgenticUrlShortner.Contracts.Client;
using CS.AgenticUrlShortner.Managers;
using CS.AgenticUrlShortner.Managers.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CS.AgenticUrlShortner.WebApiClient.Controllers
{
    public class ProcessUrlController : BaseController
    {
        public ProcessUrlController(ManagerFactory managerFactory) : base(managerFactory)
        {
        }

        [HttpPost("api/url/encode")]
        [EnableRateLimiting(Constants.EncodeRateLimitPolicy)]
        [RequestSizeLimit(Constants.MaxEncodeRequestBytes)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(EncodeUrlResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(EncodeUrlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<IActionResult> EncodeUrl([FromBody] EncodeUrlRequest request, CancellationToken cancellationToken)
        {
            return EncodeAsync(request, cancellationToken);
        }

        private async Task<IActionResult> EncodeAsync(EncodeUrlRequest request, CancellationToken cancellationToken)
        {
            if (Uri.TryCreate(request?.LongUrl, UriKind.Absolute, out var target)
                && string.Equals(target.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Validation failed.", new Dictionary<string, string[]>
                {
                    { nameof(EncodeUrlRequest.LongUrl), ["LongUrl must not point to this service."] }
                });
            }

            var response = await ManagerFactory.Create<IUrlManager>()
                .EncodeUrlAsync(request, cancellationToken).ConfigureAwait(false);

            response.ShortUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{response.Code}";

            return response.IsNew ? Created(response.ShortUrl, response) : Ok(response);
        }

        [HttpGet("{code}")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DecodeUrl(string code, CancellationToken cancellationToken)
        {
            var response = await ManagerFactory.Create<IUrlManager>()
                .DecodeUrlAsync(code, cancellationToken).ConfigureAwait(false);

            return response == null ? NotFound() : Redirect(response.LongUrl);
        }
    }
}
