using System.Net.Http.Json;
using System.Text.Json;
using MyApi.Modules.AiChat.DTOs;

namespace MyApi.Modules.AiChat.Services
{
    public class OllamaService : IOllamaService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OllamaService> _logger;
        private readonly string _ollamaBaseUrl;
        private readonly string _defaultModel;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public OllamaService(
            IHttpClientFactory httpClientFactory,
            ILogger<OllamaService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            // Configurable via appsettings or env var OLLAMA_BASE_URL
            _ollamaBaseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
                             ?? configuration.GetValue<string>("Ollama:BaseUrl")
                             ?? "http://localhost:11434";
            _defaultModel = Environment.GetEnvironmentVariable("OLLAMA_DEFAULT_MODEL")
                            ?? configuration.GetValue<string>("Ollama:DefaultModel")
                            ?? "llama3:8b";
        }

        public async Task<GenerateWishResponseDto> GenerateAsync(GenerateWishRequestDto request, string userId)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model;

            _logger.LogInformation(
                "GenerateWish: user={UserId}, model={Model}, promptLen={Len}",
                userId, model, request.Prompt.Length);

            var ollamaPayload = new OllamaGenerateRequest
            {
                Model = model,
                Prompt = request.Prompt,
                Stream = false
            };

            try
            {
                var client = _httpClientFactory.CreateClient("Ollama");
                client.BaseAddress = new Uri(_ollamaBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(120); // LLM can be slow

                var httpResponse = await client.PostAsJsonAsync("/api/generate", ollamaPayload, _jsonOptions);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Ollama returned {StatusCode}: {Body}",
                        (int)httpResponse.StatusCode, errorBody);

                    return new GenerateWishResponseDto
                    {
                        Success = false,
                        Model = model,
                        Error = $"LLM returned HTTP {(int)httpResponse.StatusCode}: {errorBody}"
                    };
                }

                var ollamaResponse = await httpResponse.Content
                    .ReadFromJsonAsync<OllamaGenerateResponse>(_jsonOptions);

                if (ollamaResponse == null || string.IsNullOrEmpty(ollamaResponse.Response))
                {
                    return new GenerateWishResponseDto
                    {
                        Success = false,
                        Model = model,
                        Error = "Empty response from LLM"
                    };
                }

                _logger.LogInformation(
                    "GenerateWish OK: model={Model}, responseLen={Len}, durationMs={Ms}",
                    ollamaResponse.Model,
                    ollamaResponse.Response.Length,
                    ollamaResponse.Total_duration / 1_000_000);

                return new GenerateWishResponseDto
                {
                    Success = true,
                    Response = ollamaResponse.Response.Trim(),
                    Model = ollamaResponse.Model,
                    TotalDurationNs = ollamaResponse.Total_duration,
                    ConversationId = request.ConversationId
                };
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Ollama request timed out for user {UserId}", userId);
                return new GenerateWishResponseDto
                {
                    Success = false,
                    Model = model,
                    Error = "Request timed out — the LLM server took too long to respond"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Cannot reach Ollama at {Url}", _ollamaBaseUrl);
                return new GenerateWishResponseDto
                {
                    Success = false,
                    Model = model,
                    Error = $"Cannot reach LLM server at {_ollamaBaseUrl}: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GenerateWish");
                return new GenerateWishResponseDto
                {
                    Success = false,
                    Model = model,
                    Error = "An unexpected error occurred while generating the response"
                };
            }
        }
    }
}
