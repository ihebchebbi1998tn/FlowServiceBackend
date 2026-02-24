using System.Net.Http.Json;
using System.Text;
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
            var hasMessages = request.Messages != null && request.Messages.Count > 0;

            _logger.LogInformation(
                "GenerateWish: user={UserId}, model={Model}, mode={Mode}",
                userId, model, hasMessages ? "chat" : "generate");

            try
            {
                var client = _httpClientFactory.CreateClient("Ollama");
                client.BaseAddress = new Uri(_ollamaBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(120);

                HttpResponseMessage httpResponse;

                if (hasMessages)
                {
                    // ── Chat mode: /api/chat ──
                    var chatPayload = new OllamaChatRequest
                    {
                        Model = model,
                        Messages = request.Messages!.Select(m => new OllamaChatMessage
                        {
                            Role = m.Role.ToLower(),
                            Content = m.Content
                        }).ToList(),
                        Stream = false,
                        Options = (request.Temperature.HasValue || request.MaxTokens.HasValue) ? new OllamaChatOptions
                        {
                            Temperature = request.Temperature,
                            Num_predict = request.MaxTokens
                        } : null
                    };

                    httpResponse = await client.PostAsJsonAsync("/api/chat", chatPayload, _jsonOptions);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await httpResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Ollama /api/chat returned {StatusCode}: {Body}", (int)httpResponse.StatusCode, errorBody);
                        return new GenerateWishResponseDto
                        {
                            Success = false, Model = model,
                            Error = $"LLM returned HTTP {(int)httpResponse.StatusCode}: {errorBody}"
                        };
                    }

                    var chatResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions);
                    if (chatResponse?.Message == null || string.IsNullOrEmpty(chatResponse.Message.Content))
                    {
                        return new GenerateWishResponseDto { Success = false, Model = model, Error = "Empty response from LLM" };
                    }

                    return new GenerateWishResponseDto
                    {
                        Success = true,
                        Response = chatResponse.Message.Content.Trim(),
                        Model = chatResponse.Model,
                        TotalDurationNs = chatResponse.Total_duration,
                        ConversationId = request.ConversationId
                    };
                }
                else
                {
                    // ── Legacy generate mode: /api/generate ──
                    if (string.IsNullOrWhiteSpace(request.Prompt))
                    {
                        return new GenerateWishResponseDto { Success = false, Model = model, Error = "Either Prompt or Messages must be provided" };
                    }

                    var generatePayload = new OllamaGenerateRequest
                    {
                        Model = model,
                        Prompt = request.Prompt!,
                        Stream = false
                    };

                    httpResponse = await client.PostAsJsonAsync("/api/generate", generatePayload, _jsonOptions);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await httpResponse.Content.ReadAsStringAsync();
                        _logger.LogError("Ollama returned {StatusCode}: {Body}", (int)httpResponse.StatusCode, errorBody);
                        return new GenerateWishResponseDto
                        {
                            Success = false, Model = model,
                            Error = $"LLM returned HTTP {(int)httpResponse.StatusCode}: {errorBody}"
                        };
                    }

                    var ollamaResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaGenerateResponse>(_jsonOptions);
                    if (ollamaResponse == null || string.IsNullOrEmpty(ollamaResponse.Response))
                    {
                        return new GenerateWishResponseDto { Success = false, Model = model, Error = "Empty response from LLM" };
                    }

                    return new GenerateWishResponseDto
                    {
                        Success = true,
                        Response = ollamaResponse.Response.Trim(),
                        Model = ollamaResponse.Model,
                        TotalDurationNs = ollamaResponse.Total_duration,
                        ConversationId = request.ConversationId
                    };
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Ollama request timed out for user {UserId}", userId);
                return new GenerateWishResponseDto { Success = false, Model = model, Error = "Request timed out — the LLM server took too long to respond" };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Cannot reach Ollama at {Url}", _ollamaBaseUrl);
                return new GenerateWishResponseDto { Success = false, Model = model, Error = $"Cannot reach LLM server at {_ollamaBaseUrl}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GenerateWish");
                return new GenerateWishResponseDto { Success = false, Model = model, Error = "An unexpected error occurred while generating the response" };
            }
        }

        /// <summary>
        /// Streams chat response as SSE events. Each Ollama chunk is forwarded as a data: line.
        /// </summary>
        public async Task StreamChatAsync(GenerateWishRequestDto request, string userId, Stream responseStream, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model;

            try
            {
                var client = _httpClientFactory.CreateClient("Ollama");
                client.BaseAddress = new Uri(_ollamaBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(120);

                var messages = (request.Messages ?? new List<ChatMessageDto>())
                    .Select(m => new OllamaChatMessage { Role = m.Role.ToLower(), Content = m.Content })
                    .ToList();

                if (messages.Count == 0 && !string.IsNullOrWhiteSpace(request.Prompt))
                {
                    messages.Add(new OllamaChatMessage { Role = "user", Content = request.Prompt! });
                }

                var chatPayload = new OllamaChatRequest
                {
                    Model = model,
                    Messages = messages,
                    Stream = true,
                    Options = (request.Temperature.HasValue || request.MaxTokens.HasValue) ? new OllamaChatOptions
                    {
                        Temperature = request.Temperature,
                        Num_predict = request.MaxTokens
                    } : null
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(chatPayload, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat") { Content = jsonContent };
                var httpResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    var errorEvent = JsonSerializer.Serialize(new { error = $"LLM error {(int)httpResponse.StatusCode}: {errorBody}" });
                    await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                    await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
                    return;
                }

                using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaChatStreamChunk>(line, _jsonOptions);
                        if (chunk?.Message?.Content != null)
                        {
                            var sseData = JsonSerializer.Serialize(new
                            {
                                choices = new[]
                                {
                                    new
                                    {
                                        delta = new { content = chunk.Message.Content },
                                        index = 0
                                    }
                                },
                                model = chunk.Model
                            });
                            await WriteSseLineAsync(responseStream, $"data: {sseData}", cancellationToken);
                        }

                        if (chunk?.Done == true)
                        {
                            await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
                            break;
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed chunks
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Streaming error for user {UserId}", userId);
                var errorEvent = JsonSerializer.Serialize(new { error = ex.Message });
                await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
            }
        }

        /// <summary>
        /// Write a single SSE line to the response stream without synchronous IO.
        /// </summary>
        private static async Task WriteSseLineAsync(Stream stream, string line, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            await stream.WriteAsync(bytes, 0, bytes.Length, ct);
            await stream.FlushAsync(ct);
        }
    }
}
