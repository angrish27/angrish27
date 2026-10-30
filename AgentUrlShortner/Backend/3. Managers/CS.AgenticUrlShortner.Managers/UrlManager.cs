using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Accessors.Contracts;
using CS.AgenticUrlShortner.Contracts.Client;
using CS.AgenticUrlShortner.Contracts.Logic;
using CS.AgenticUrlShortner.Engines.Contracts;
using CS.AgenticUrlShortner.Managers.Contracts;
using CS.AgenticUrlShortner.Utilities.Contracts;

namespace CS.AgenticUrlShortner.Managers
{
    internal sealed class UrlManager : ManagerBase, IUrlManager
    {
        public async Task<EncodeUrlResponse> EncodeUrlAsync(EncodeUrlRequest request, CancellationToken cancellationToken = default)
        {
            var longUrl = request?.LongUrl;

            EngineFactory.Create<IUrlValidationEngine>(AccessorFactory, UtilityFactory).ValidateLongUrl(longUrl);

            var accessor = AccessorFactory.Create<IUrlMappingAccessor>(UtilityFactory);

            var existing = accessor.FindByLongUrl(longUrl);
            if (existing != null)
                return ToEncodeResponse(existing, false);

            var (mapping, created) = await accessor.GetOrCreateAsync(longUrl, cancellationToken).ConfigureAwait(false);
            return ToEncodeResponse(mapping, created);
        }

        public Task<DecodeUrlResponse> DecodeUrlAsync(string code, CancellationToken cancellationToken = default)
        {
            if (!UtilityFactory.Create<IBase62Utility>().IsValid(code))
                return Task.FromResult<DecodeUrlResponse>(null);

            var mapping = AccessorFactory.Create<IUrlMappingAccessor>(UtilityFactory).FindByCode(code);

            return Task.FromResult(mapping == null ? null : new DecodeUrlResponse { LongUrl = mapping.ActualLongUrl });
        }

        private static EncodeUrlResponse ToEncodeResponse(UrlMapping mapping, bool isNew) => new()
        {
            LongUrl = mapping.ActualLongUrl,
            Code = mapping.Base62,
            IsNew = isNew
        };
    }
}
