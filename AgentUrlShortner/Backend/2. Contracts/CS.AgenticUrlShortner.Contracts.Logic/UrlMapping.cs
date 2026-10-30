using System.Text.Json.Serialization;

namespace CS.AgenticUrlShortner.Contracts.Logic
{
    public class UrlMapping
    {
        [JsonPropertyName("seqNumber")]
        public long SeqNumber { get; set; }

        [JsonPropertyName("actualLongURL")]
        public string ActualLongUrl { get; set; }

        [JsonPropertyName("base62")]
        public string Base62 { get; set; }
    }
}
