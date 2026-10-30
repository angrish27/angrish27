using System.Text.Json.Serialization;

namespace CS.AgenticUrlShortner.Contracts.Logic
{
    public class UrlMapping
    {
        [JsonPropertyName("seqNumber")]
        public long SeqNumber { get; set; }

        [JsonPropertyName("actualLongURL")]
        public string ActualLongUrl { get; set; }

        [JsonPropertyName("shortCode")]
        public string ShortCode { get; set; }

        [JsonPropertyName("codeEpochNanoseconds")]
        public long? CodeEpochNanoseconds { get; set; }

        [JsonPropertyName("base62")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string LegacyBase62
        {
            get => null;
            set => ShortCode ??= value;
        }
    }
}
