using LocalAIInteractions.Chat;
using LocalAIInteractions.Images;
using LocalAIInteractions.Model;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalAIInteractions
{
    public class APIClient
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Dictionary<string, string> _mimeTypes = new Dictionary<string, string>()
        {
            { "jpeg", "jpeg" },
            { "jpg", "jpeg" },
            { "png", "png" },
            { "gif", "gif" }
        };

        public string? Hostname { get; set; }

        public string? Port { get; set; }

        /// <summary>
        /// HttpClient Timeout in seconds
        /// </summary>
        public int Timeout { get; set; }

        public APIClient()
        {

        }
        public APIClient(string hostName, string port)
        {
            Hostname = hostName;
            Port = port;
        }

        public APIClient(string hostName, string port, int timeout)
        {
            Hostname = hostName;
            Port = port;
            Timeout = timeout;
        }

        /// <summary>
        /// Sends a chat request to the endpoint
        /// </summary>
        /// <param name="message">Message to be sent to chat</param>
        /// <param name="temperature">Temperature to add, default 0.7</param>
        /// <param name="existingConversation">Any existing conversation to provide context</param>
        /// <returns>Message object with the response and role</returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<Message> Chat(string message, string model = null, double temperature = 0.7, ChatConversation existingConversation = null, string apiKey = null)
        {
            CheckEndpointVariables();
            model = model ?? Models.Gemma2;

            using (var client = new HttpClient())
            {
                if (Timeout > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(Timeout);
                }

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    //api key usage infers openwebui usage, adjust api endpoint as necessary
                    Endpoints.Endpoints.Version = "api";
                }

                var request = new ChatRequest()
                {
                    Model = model,
                    Temperature = temperature
                };
                var userMessage = new Message()
                {
                    Role = Role.User,
                    Content = message
                };
                //insert existing conversation if it exists
                if (existingConversation != null)
                {
                    List<Message> messagesCopy = new(existingConversation.Messages)
                    {
                        userMessage
                    };
                    request.Messages = messagesCopy.ToArray();
                }
                else
                {
                    request.Messages = [userMessage];
                }
                var payload = JsonSerializer.Serialize(request, _serializerOptions);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage? response = await client.PostAsync($"{Hostname}:{Port}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Chat}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var deserialized = JsonSerializer.Deserialize<ChatResponse>(await response.Content.ReadAsStreamAsync());
                        var receivedMessage = deserialized.Choices[0].Message;
                        return new Message() { Content = receivedMessage.Content, Role = receivedMessage.Role };
                    }
                    else
                    {
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                        throw new Exception($"Chat request failed with status code: {response.StatusCode}");
                    }
                }
                catch (InvalidOperationException ioEx)
                {

                }
                catch (HttpRequestException httpEx)
                {
                    throw httpEx;
                }
                return null;
            }
        }

        private void CheckEndpointVariables()
        {
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                throw new ArgumentException("Hostname must be set");
            }

            if (string.IsNullOrWhiteSpace(Port))
            {
                throw new ArgumentException("Port must be set");
            }

            if (!Uri.IsWellFormedUriString($"{Hostname}:{Port}", UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("Hostname must be a well formed URI");
            }
        }


        /// <summary>
        /// Sends an image to the endpoint do be described
        /// </summary>
        /// <param name="imagePath">Filepath or URL of the image to process</param>
        /// <param name="prompt">Prompt for the endpoint to process, defaults to describe the image</param>
        /// <param name="model">Selected model to use, defaults to llava</param>
        /// <returns>Completed message</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<Message> RecognizeImage(string imagePath, string prompt = null, string model = null, string apiKey = null)
        {
            CheckEndpointVariables();
            model = string.IsNullOrWhiteSpace(model) ? "gpt-4o" : model;
            prompt = string.IsNullOrWhiteSpace(prompt) ? "Describe this image" : prompt;
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentNullException(imagePath);
            }
            bool isUrl = false;
            if (!(new Uri(imagePath)).Scheme.Equals("file", StringComparison.OrdinalIgnoreCase) && Uri.IsWellFormedUriString(imagePath, UriKind.RelativeOrAbsolute))
            {
                isUrl = true;
            }
            else if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException(imagePath);
            }

            using (var client = new HttpClient())
            {
                if (Timeout > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(Timeout);
                }

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    //api key usage infers openwebui usage, adjust api endpoint as necessary
                    Endpoints.Endpoints.Version = "api";
                }
                var imageContent = new ImageContent()
                {
                    Type = "image_url"
                };

                if (isUrl)
                {
                    imageContent.Image = new ImageUrl()
                    {
                        Url = imagePath
                    };
                }
                else
                {
                    var extension = Path.GetExtension(imagePath).Substring(1);
                    var mimeType = _mimeTypes[extension];
                    var imageBytes = Utility.Image.EncodeImageToBase64(imagePath);
                    imageContent.Image = new ImageUrl()
                    {
                        Url = $"data:image/{mimeType};base64,{imageBytes}"
                    };
                }

                var imageRequest = new ImageChatRequest()
                {
                    Model = model,
                    Messages =
                    [
                        new ImageMessage(){
                            Role = Role.User,
                            Content =
                            [
                                new ImageContent(){
                                    Type = "text",
                                    Text = prompt
                                },
                                imageContent
                            ]
                        }
                    ]
                };

                var imagePayload = JsonSerializer.Serialize(imageRequest, _serializerOptions);
                var content = new StringContent(imagePayload, Encoding.UTF8, "application/json");

                HttpResponseMessage? response = await client.PostAsync($"{Hostname}:{Port}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Chat}", content);

                if (response.IsSuccessStatusCode)
                {
                    var deserialized = JsonSerializer.Deserialize<ChatResponse>(await response.Content.ReadAsStreamAsync());
                    return new Message() { Content = deserialized.Choices[0].Message.Content, Role = Role.Assistant };
                }
                else
                {
                    throw new Exception($"Image recognition request failed with status code: {response.StatusCode}");
                }
            }
        }


        /// <summary>
        /// Generates an image from the prompt
        /// </summary>
        /// <param name="prompt">Description of the image to be generated</param>
        /// <param name="steps">Steps for the algorithm</param>
        /// <returns>URL of the generated image</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task<string> GenerateImage(string prompt, int steps = 25, string apiKey = null)
        {
            CheckEndpointVariables();

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentNullException(nameof(prompt));
            }
            using (var client = new HttpClient())
            {
                if (Timeout > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(Timeout);
                }
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    //api key usage infers openwebui usage, adjust api endpoint as necessary
                    Endpoints.Endpoints.Version = "api";
                }

                var imageRequest = new ImageRequest()
                {
                    Model = Models.StableDiffisuion,
                    N = 1,
                    Prompt = prompt,
                    Steps = steps
                };

                var payload = JsonSerializer.Serialize(imageRequest, _serializerOptions);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{Hostname}:{Port}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Image}", content);
                if (response.IsSuccessStatusCode)
                {
                    var deserialized = JsonSerializer.Deserialize<ImageResponse>(await response.Content.ReadAsStreamAsync());
                    return deserialized?.Data[0].URL;
                }
                else
                {
                    throw new Exception($"Image generation request failed with status code: {response.StatusCode}");
                }
            }
        }
    }
}
