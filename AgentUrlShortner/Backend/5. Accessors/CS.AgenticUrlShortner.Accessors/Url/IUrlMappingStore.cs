using System;
using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Contracts.Logic;

namespace CS.AgenticUrlShortner.Accessors.Url
{
    /// <summary>
    /// Persistence boundary for URL mappings. The default implementation is a single-instance JSON file store;
    /// a database-backed implementation can replace it by changing the registration in AccessorFactory.RegisterTypes.
    /// See docs/decisions.md (ADR-001).
    /// </summary>
    internal interface IUrlMappingStore
    {
        void Load(string location);

        UrlMapping FindByLongUrl(string longUrl);

        UrlMapping FindByCode(string code);

        Task<(UrlMapping Mapping, bool Created)> GetOrAddAsync(string longUrl, long requestEpochNanoseconds,
            Func<long, string> encode, CancellationToken cancellationToken);
    }
}
