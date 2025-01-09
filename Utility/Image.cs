using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;

namespace LocalAIInteractions.Utility
{
    public static class Image
    {
        public static string EncodeImageToBase64(string path)
        {
            byte[] imageArray = File.ReadAllBytes(path);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            return base64ImageRepresentation;
        }

        public static string[] VideoToImageFrames(string filePath, int frameCount)
        {
            var video = new MediaFile { Filename = filePath };
            var directory = Path.GetDirectoryName(filePath);
            
            using (var engine = new Engine())
            {
                engine.GetMetadata(video);
                var totalTime = video.Metadata.Duration;
                var totalSeconds = (int)Math.Floor(totalTime.TotalSeconds);
                //string[] frames = new string[totalSeconds];
                string[] frames = new string[2];
                ConversionOptions options = new ConversionOptions();
                options.CustomHeight = 512;
                options.CustomWidth = 512;
                options.VideoSize = VideoSize.Custom;
                for (int i = 0; i < frames.Length; i++)
                {
                    options.Seek = TimeSpan.FromSeconds(i);
                    string outputFileName = $"{Path.GetRandomFileName()}.jpg";
                    MediaFile outputFile = new MediaFile { Filename = outputFileName };
                    engine.GetThumbnail(video, outputFile, options);
                    frames[i] = outputFileName;
                }
                return frames;
            }
        }
    }
}
