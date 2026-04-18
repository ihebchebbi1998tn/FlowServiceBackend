using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.ExternalEndpoints.Services;
using System.Text.Json;

namespace MyApi.Modules.ExternalEndpoints.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/external-receive")]
    public class ExternalReceiveController : ControllerBase
    {
        private readonly IExternalEndpointService _service;
        private readonly ILogger<ExternalReceiveController> _logger;

        public ExternalReceiveController(IExternalEndpointService service, ILogger<ExternalReceiveController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("{slug}")]
        [HttpGet("{slug}")]
        [HttpPut("{slug}")]
        public async Task<IActionResult> Receive(string slug)
        {
            try
            {
                var method = Request.Method;
                var apiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
                var queryString = Request.QueryString.Value;

                // Collect headers
                var headers = JsonSerializer.Serialize(
                    Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                );

                // Read body
                string? body = null;
                if (method != "GET")
                {
                    using var reader = new StreamReader(Request.Body);
                    body = await reader.ReadToEndAsync();
                }

                var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var (statusCode, responseBody) = await _service.ReceiveAsync(slug, method, headers, queryString, body, sourceIp, apiKey);

                // Detect JSON vs plain text so user-defined ResponseTemplate values never crash the endpoint.
                var raw = responseBody ?? string.Empty;
                var trimmed = raw.TrimStart();
                var looksLikeJson = trimmed.StartsWith("{") || trimmed.StartsWith("[") || trimmed.StartsWith("\"");
                return new ContentResult
                {
                    Content = raw,
                    ContentType = looksLikeJson ? "application/json" : "text/plain",
                    StatusCode = statusCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving data for slug {Slug}", slug);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
