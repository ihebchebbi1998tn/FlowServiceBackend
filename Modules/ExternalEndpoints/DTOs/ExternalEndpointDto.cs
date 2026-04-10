namespace MyApi.Modules.ExternalEndpoints.DTOs
{
    public class ExternalEndpointDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string AllowedMethods { get; set; } = "POST";
        public string? AllowedOrigins { get; set; }
        public string? ExpectedSchema { get; set; }
        public string? ResponseTemplate { get; set; }
        public string? WebhookForwardUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int TotalReceived { get; set; }
        public int ReceivedToday { get; set; }
        public DateTime? LastReceived { get; set; }
    }

    public class CreateExternalEndpointDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string AllowedMethods { get; set; } = "POST";
        public string? AllowedOrigins { get; set; }
        public string? ExpectedSchema { get; set; }
        public string? ResponseTemplate { get; set; }
        public string? WebhookForwardUrl { get; set; }
    }

    public class UpdateExternalEndpointDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public string? AllowedMethods { get; set; }
        public string? AllowedOrigins { get; set; }
        public string? ExpectedSchema { get; set; }
        public string? ResponseTemplate { get; set; }
        public string? WebhookForwardUrl { get; set; }
    }

    public class ExternalEndpointLogDto
    {
        public int Id { get; set; }
        public int EndpointId { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Headers { get; set; }
        public string? QueryString { get; set; }
        public string? Body { get; set; }
        public string? SourceIp { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class ExternalEndpointStatsDto
    {
        public int TotalEndpoints { get; set; }
        public int ActiveEndpoints { get; set; }
        public int TotalReceivedToday { get; set; }
        public int TotalReceivedAll { get; set; }
    }

    public class PaginatedEndpointResponse
    {
        public List<ExternalEndpointDto> Endpoints { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginatedLogResponse
    {
        public List<ExternalEndpointLogDto> Logs { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }
}
