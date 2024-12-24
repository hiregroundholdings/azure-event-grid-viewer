namespace Viewer.Models
{
    using System.Text.Json.Serialization;

    // Reference: https://github.com/cloudevents/spec/tree/v1.0-rc1 

    public class CloudEvent<T> where T : class
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonIgnore]
        public string RawEvent { get; set; }
    }
}
