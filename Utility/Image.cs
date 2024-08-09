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
    }
}
