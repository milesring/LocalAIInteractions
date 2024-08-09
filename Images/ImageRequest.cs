using System.Text.Json.Serialization;

namespace LocalAIInteractions.Images
{
    public class ImageRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("n")]
        public int N { get; set; }
        [JsonPropertyName(name: "prompt")]
        public string Prompt { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; } = "512x512";
        [JsonPropertyName("steps")]
        public int Steps { get; set; } = 20;
    }
}
