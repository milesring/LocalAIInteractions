namespace LocalAIInteractions.Endpoints
{
    public static class Endpoints
    {
        private static string _version = "v1";
        public static string Version { get => _version; set => _version = value; }
        private static string _chat = "chat/completions";
        public static string Chat { get => _chat; set => _chat = value; }
        private static string _image = "images/generations";
        public static string Image { get => _image; set => _image = value; }
        private static string _tts = "text-to-speech/";
        public static string TTS { get => _tts; set => _tts = value; }

    }
}
