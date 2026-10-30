using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Accessors.Contracts;
using CS.AgenticUrlShortner.Contracts.Logic;
using CS.AgenticUrlShortner.Utilities.Contracts;

namespace CS.AgenticUrlShortner.Accessors.Url
{
    internal sealed class UrlMappingAccessor : AccessorBase, IUrlMappingAccessor
    {
        private IUrlMappingStore Store => Factory.ResolveRequiredService<IUrlMappingStore>();

        public UrlMapping FindByLongUrl(string longUrl)
        {
            return Store.FindByLongUrl(longUrl);
        }

        public UrlMapping FindByCode(string code)
        {
            return Store.FindByCode(code);
        }

        public Task<(UrlMapping Mapping, bool Created)> GetOrCreateAsync(string longUrl, long requestEpochNanoseconds,
            CancellationToken cancellationToken = default)
        {
            var shortCode = UtilityFactory.Create<IShortCodeUtility>();
            return Store.GetOrAddAsync(longUrl, requestEpochNanoseconds,
                shortCode.EncodeUnixEpochNanoseconds, cancellationToken);
        }
    }
}
