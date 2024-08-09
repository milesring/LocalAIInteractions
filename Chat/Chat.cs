using System.Text.Json.Serialization;

namespace LocalAIInteractions.Chat
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ImageMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        [JsonPropertyName("content")]
        public ImageContent[] Content { get; set; }

    }

    public class ImageContent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("image_url")]
        public ImageUrl Image { get; set; }
    }

    public class ImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public static class Role
    {
        public static string User { get => "user"; }
        public static string System { get => "system"; }
        public static string Assistant { get => "assistant"; }
    }
}
