using System.Threading;
using System.Threading.Tasks;
using System;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Contracts.Client;

namespace CS.AgenticUrlShortner.Managers.Contracts
{
    public interface IUrlManager : IService
    {
        Task<EncodeUrlResponse> EncodeUrlAsync(EncodeUrlRequest request, CancellationToken cancellationToken = default,
            DateTimeOffset? requestReceivedAtUtc = null);

        Task<DecodeUrlResponse> DecodeUrlAsync(string code, CancellationToken cancellationToken = default);
    }
}
