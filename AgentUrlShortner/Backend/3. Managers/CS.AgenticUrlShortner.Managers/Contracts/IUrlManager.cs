using System.Threading;
using System.Threading.Tasks;
using CS.AgenticUrlShortner.Common.Contracts;
using CS.AgenticUrlShortner.Contracts.Client;

namespace CS.AgenticUrlShortner.Managers.Contracts
{
    public interface IUrlManager : IService
    {
        Task<EncodeUrlResponse> EncodeUrlAsync(EncodeUrlRequest request, CancellationToken cancellationToken = default);

        Task<DecodeUrlResponse> DecodeUrlAsync(string code, CancellationToken cancellationToken = default);
    }
}
