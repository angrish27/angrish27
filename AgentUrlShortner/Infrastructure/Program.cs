using System.Threading.Tasks;
using Pulumi;

namespace CS.AgenticUrlShortner.Infrastructure
{
    internal static class Program
    {
        private static Task<int> Main() => Deployment.RunAsync<MyStack>();
    }
}
