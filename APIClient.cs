using LocalAIInteractions.Chat;
using LocalAIInteractions.Images;
using LocalAIInteractions.Model;
using System.Net.Http.Headers;
using System.Net.WebSockets;
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
        public APIClient(string hostName)
        {
            Hostname = hostName;
        }

        public APIClient(string hostName, int timeout)
        {
            Hostname = hostName;
            Timeout = timeout;
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
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<Message> Chat(string message, string model = null, double temperature = 0.7, ChatConversation existingConversation = null, string apiKey = null, List<Chat.File> files = null)
        {
            CheckEndpointVariables();
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentNullException(nameof(model), "Model must be set");
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

                var request = new ChatRequest()
                {
                    Model = model,
                    Temperature = temperature
                };

                if (files != null && files.Count > 0)
                {
                    request.Files = files.ToArray();
                }
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
                    string host = $"{Hostname}{(string.IsNullOrWhiteSpace(Port) ? string.Empty : $":{Port}")}";
                    HttpResponseMessage? response = await client.PostAsync($"{host}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Chat}", content);

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

            string host = $"{Hostname}{(string.IsNullOrWhiteSpace(Port) ? string.Empty : $":{Port}")}";

            if (!Uri.IsWellFormedUriString(host, UriKind.RelativeOrAbsolute))
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
            else if (!System.IO.File.Exists(imagePath))
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

                string host = $"{Hostname}{(string.IsNullOrWhiteSpace(Port) ? string.Empty : $":{Port}")}";
                HttpResponseMessage? response = await client.PostAsync($"{host}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Chat}", content);

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
                    Endpoints.Endpoints.Version = "api/v1";
                }

                var imageRequest = new ImageRequest()
                {
                    Prompt = prompt,
                    N = 1,
                    Size = "512x512",
                    Steps = 50
                };

                var payload = JsonSerializer.Serialize(imageRequest, _serializerOptions);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                string host = $"{Hostname}{(string.IsNullOrWhiteSpace(Port) ? string.Empty : $":{Port}")}";
                var response = await client.PostAsync($"{host}/{Endpoints.Endpoints.Version}/{Endpoints.Endpoints.Image}", content);
                if (response.IsSuccessStatusCode)
                {
                    var deserialized = JsonSerializer.Deserialize<OpenWebUIImageResponse[]>(await response.Content.ReadAsStreamAsync());

                    return $"{host}{deserialized?[0].Url}";
                }
                else
                {
                    throw new Exception($"Image generation request failed with status code: {response.StatusCode}");
                }
            }
        }

    }
}
