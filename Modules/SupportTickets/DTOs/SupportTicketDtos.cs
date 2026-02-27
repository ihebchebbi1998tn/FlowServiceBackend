using Microsoft.AspNetCore.Http;

namespace MyApi.Modules.SupportTickets.DTOs
{
    public class CreateSupportTicketDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Urgency { get; set; }
        public string? Category { get; set; }
        public string? CurrentPage { get; set; }
        public string? RelatedUrl { get; set; }
        public string? UserEmail { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }

    public class SupportTicketResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Urgency { get; set; }
        public string? Category { get; set; }
        public string? CurrentPage { get; set; }
        public string? RelatedUrl { get; set; }
        public string Tenant { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string Status { get; set; } = "open";
        public DateTime CreatedAt { get; set; }
        public List<SupportTicketAttachmentDto> Attachments { get; set; } = new();
    }

    public class SupportTicketAttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
