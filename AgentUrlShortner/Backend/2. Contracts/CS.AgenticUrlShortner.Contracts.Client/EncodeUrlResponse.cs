using System.Text.Json.Serialization;

namespace CS.AgenticUrlShortner.Contracts.Client
{
    public class EncodeUrlResponse
    {
        [JsonIgnore]
        public string LongUrl { get; set; }

        [JsonIgnore]
        public string Code { get; set; }

        public string ShortUrl { get; set; }

        [JsonIgnore]
        public bool IsNew { get; set; }
    }
}
