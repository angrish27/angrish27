using System.Security.Claims;

namespace CS.AgenticUrlShortner.Common.Contracts
{
    public class AmbientContext
    {
        public string CorrelationId { get; set; }

        public string UserId { get; set; }

        public string FullName { get; set; }

        public string[] Groups { get; set; }

        public Claim[] Claims { get; set; }
    }
}
