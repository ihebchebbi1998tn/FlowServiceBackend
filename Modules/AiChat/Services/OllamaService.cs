using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MyApi.Modules.AiChat.DTOs;
using System.Diagnostics;

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
                            ?? "mistral";

            _logger.LogInformation("🤖 OllamaService initialized — BaseUrl={BaseUrl}, DefaultModel={Model}", _ollamaBaseUrl, _defaultModel);
        }

        public async Task<GenerateWishResponseDto> GenerateAsync(GenerateWishRequestDto request, string userId)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model;
            var hasMessages = request.Messages != null && request.Messages.Count > 0;
            var sw = Stopwatch.StartNew();

            _logger.LogInformation(
                "📨 GenerateWish: user={UserId}, model={Model}, mode={Mode}, msgCount={MsgCount}",
                userId, model, hasMessages ? "chat" : "generate",
                hasMessages ? request.Messages!.Count : 0);

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
                        Options = new OllamaChatOptions
                        {
                            Temperature = request.Temperature ?? 0.7f,
                            Num_predict = request.MaxTokens ?? 2048
                        }
                    };

                    _logger.LogDebug("🔄 POST /api/chat — model={Model}, messages={Count}", model, chatPayload.Messages.Count);

                    httpResponse = await client.PostAsJsonAsync("/api/chat", chatPayload, _jsonOptions);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await httpResponse.Content.ReadAsStringAsync();
                        _logger.LogError("❌ Ollama /api/chat {StatusCode}: {Body}", (int)httpResponse.StatusCode, errorBody);
                        return new GenerateWishResponseDto
                        {
                            Success = false, Model = model,
                            Error = $"LLM returned HTTP {(int)httpResponse.StatusCode}: {errorBody}"
                        };
                    }

                    var chatResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaChatResponse>(_jsonOptions);
                    if (chatResponse?.Message == null || string.IsNullOrEmpty(chatResponse.Message.Content))
                    {
                        _logger.LogWarning("⚠️ Empty response from Ollama chat — user={UserId}, elapsed={Ms}ms", userId, sw.ElapsedMilliseconds);
                        return new GenerateWishResponseDto { Success = false, Model = model, Error = "Empty response from LLM" };
                    }

                    sw.Stop();
                    _logger.LogInformation("✅ GenerateWish OK — user={UserId}, model={Model}, ollamaDuration={OllamaMs}ms, totalElapsed={TotalMs}ms, responseLen={Len}",
                        userId, chatResponse.Model, chatResponse.Total_duration / 1_000_000, sw.ElapsedMilliseconds, chatResponse.Message.Content.Length);

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

                    _logger.LogDebug("🔄 POST /api/generate — model={Model}", model);

                    httpResponse = await client.PostAsJsonAsync("/api/generate", generatePayload, _jsonOptions);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        var errorBody = await httpResponse.Content.ReadAsStringAsync();
                        _logger.LogError("❌ Ollama /api/generate {StatusCode}: {Body}", (int)httpResponse.StatusCode, errorBody);
                        return new GenerateWishResponseDto
                        {
                            Success = false, Model = model,
                            Error = $"LLM returned HTTP {(int)httpResponse.StatusCode}: {errorBody}"
                        };
                    }

                    var ollamaResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaGenerateResponse>(_jsonOptions);
                    if (ollamaResponse == null || string.IsNullOrEmpty(ollamaResponse.Response))
                    {
                        _logger.LogWarning("⚠️ Empty response from Ollama generate — user={UserId}", userId);
                        return new GenerateWishResponseDto { Success = false, Model = model, Error = "Empty response from LLM" };
                    }

                    sw.Stop();
                    _logger.LogInformation("✅ GenerateWish OK — user={UserId}, model={Model}, ollamaDuration={OllamaMs}ms, totalElapsed={TotalMs}ms",
                        userId, ollamaResponse.Model, ollamaResponse.Total_duration / 1_000_000, sw.ElapsedMilliseconds);

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
                _logger.LogWarning("⏱️ Timeout after {Ms}ms — user={UserId}", sw.ElapsedMilliseconds, userId);
                return new GenerateWishResponseDto { Success = false, Model = model, Error = "Request timed out — the LLM server took too long to respond" };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "🔌 Cannot reach Ollama at {Url} — elapsed={Ms}ms", _ollamaBaseUrl, sw.ElapsedMilliseconds);
                return new GenerateWishResponseDto { Success = false, Model = model, Error = $"Cannot reach LLM server at {_ollamaBaseUrl}: {ex.Message}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error in GenerateWish — elapsed={Ms}ms", sw.ElapsedMilliseconds);
                return new GenerateWishResponseDto { Success = false, Model = model, Error = "An unexpected error occurred while generating the response" };
            }
        }

        /// <summary>
        /// Streams chat response as SSE events — fully async I/O.
        /// </summary>
        public async Task StreamChatAsync(GenerateWishRequestDto request, string userId, Stream responseStream, CancellationToken cancellationToken)
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model;
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("🔄 StreamChat START — user={UserId}, model={Model}", userId, model);

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

                _logger.LogDebug("📤 StreamChat sending {Count} messages to Ollama", messages.Count);

                var chatPayload = new OllamaChatRequest
                {
                    Model = model,
                    Messages = messages,
                    Stream = true,
                    Options = new OllamaChatOptions
                    {
                        Temperature = request.Temperature ?? 0.7f,
                        Num_predict = request.MaxTokens ?? 2048
                    }
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
                    _logger.LogError("❌ StreamChat Ollama error {StatusCode}: {Body}", (int)httpResponse.StatusCode, errorBody);
                    var errorEvent = JsonSerializer.Serialize(new { error = $"LLM error {(int)httpResponse.StatusCode}: {errorBody}" });
                    await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                    await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
                    return;
                }

                _logger.LogDebug("📥 StreamChat connected to Ollama — first byte at {Ms}ms", sw.ElapsedMilliseconds);

                using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096);
                int chunkCount = 0;

                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaChatStreamChunk>(line, _jsonOptions);
                        if (chunk?.Message?.Content != null)
                        {
                            chunkCount++;
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
                            sw.Stop();
                            _logger.LogInformation("✅ StreamChat DONE — user={UserId}, chunks={ChunkCount}, elapsed={Ms}ms", userId, chunkCount, sw.ElapsedMilliseconds);
                            await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
                            break;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogDebug("⚠️ StreamChat malformed chunk: {Error}", ex.Message);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("⏱️ StreamChat timeout after {Ms}ms — user={UserId}", sw.ElapsedMilliseconds, userId);
                var errorEvent = JsonSerializer.Serialize(new { error = "Stream request timed out" });
                await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "🔌 StreamChat cannot reach Ollama at {Url}", _ollamaBaseUrl);
                var errorEvent = JsonSerializer.Serialize(new { error = $"Cannot reach LLM: {ex.Message}" });
                await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 StreamChat error after {Ms}ms — user={UserId}", sw.ElapsedMilliseconds, userId);
                var errorEvent = JsonSerializer.Serialize(new { error = ex.Message });
                await WriteSseLineAsync(responseStream, $"data: {errorEvent}", cancellationToken);
                await WriteSseLineAsync(responseStream, "data: [DONE]", cancellationToken);
            }
        }

        /// <summary>
        /// Write a single SSE line — fully async, no synchronous IO.
        /// </summary>
        private static async Task WriteSseLineAsync(Stream stream, string line, CancellationToken ct)
        {
            var bytes = Encoding.UTF8.GetBytes(line + "\n");
            await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), ct);
            await stream.FlushAsync(ct);
        }
    }
}