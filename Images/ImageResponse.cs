using LocalAIInteractions.Chat;
using System.Text.Json.Serialization;

namespace LocalAIInteractions.Images
{
    public class ImageResponse
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("data")]
        public ImageData[] Data { get; set; }
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class ImageData
    {
        [JsonPropertyName(name: "embedding")]
        public string Embedding { get; set; }
        [JsonPropertyName(name: "index")]
        public int Index { get; set; }
        [JsonPropertyName(name: "url")]
        public string URL { get; set; }
    }

    public class OpenWebUIImageResponse
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

}
