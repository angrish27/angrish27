using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Contracts.Logic;

namespace CS.AgenticUrlShortner.Accessors.Contracts
{
    public interface IUrlMappingAccessor : IService
    {
        UrlMapping FindByLongUrl(string longUrl);

        UrlMapping FindByCode(string code);

        Task<(UrlMapping Mapping, bool Created)> GetOrCreateAsync(string longUrl, CancellationToken cancellationToken = default);
    }
}
