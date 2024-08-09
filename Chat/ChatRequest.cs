using System.Text.Json.Serialization;

namespace LocalAIInteractions.Chat
{
    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; }
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    public class ImageChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public ImageMessage[] Messages { get; set; }
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }


}