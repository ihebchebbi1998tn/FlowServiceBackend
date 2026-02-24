using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Infrastructure;
using MyApi.Modules.SupportTickets.DTOs;
using MyApi.Modules.SupportTickets.Models;

namespace MyApi.Modules.SupportTickets.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportTicketsController : ControllerBase
    {
        private readonly ITenantDbContextFactory _dbFactory;
        private readonly ILogger<SupportTicketsController> _logger;

        public SupportTicketsController(
            ITenantDbContextFactory dbFactory,
            ILogger<SupportTicketsController> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        private string GetTenant() =>
            Request.Headers.TryGetValue("X-Tenant", out var t) ? t.ToString() : "unknown";

        /// <summary>
        /// POST /api/SupportTickets — Create a new support ticket (anonymous).
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SupportTicketResponseDto>> Create([FromForm] CreateSupportTicketDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { error = "Title is required" });
            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { error = "Description is required" });

            var tenant = GetTenant();
            await using var db = _dbFactory.CreateDbContext(tenant);

            var ticket = new SupportTicket
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Urgency = dto.Urgency,
                Category = dto.Category,
                CurrentPage = dto.CurrentPage,
                RelatedUrl = dto.RelatedUrl,
                Tenant = tenant,
                UserEmail = dto.UserEmail,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            db.SupportTickets.Add(ticket);
            await db.SaveChangesAsync();

            // Handle file uploads
            if (dto.Attachments != null && dto.Attachments.Count > 0)
            {
                var uploadDir = Path.Combine("wwwroot", "uploads", "support-tickets", ticket.Id.ToString());
                Directory.CreateDirectory(uploadDir);

                foreach (var file in dto.Attachments.Take(5))
                {
                    if (file.Length == 0) continue;

                    var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadDir, safeFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var attachment = new SupportTicketAttachment
                    {
                        SupportTicketId = ticket.Id,
                        FileName = file.FileName,
                        FilePath = $"/uploads/support-tickets/{ticket.Id}/{safeFileName}",
                        FileSize = file.Length,
                        ContentType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    };

                    db.SupportTicketAttachments.Add(attachment);
                }

                await db.SaveChangesAsync();
            }

            var created = await BuildResponseDto(db, ticket.Id);

            _logger.LogInformation("Support ticket #{TicketId} created by {Email} on tenant {Tenant}",
                created.Id, created.UserEmail ?? "anonymous", tenant);

            return Created($"/api/SupportTickets/{created.Id}", created);
        }

        /// <summary>
        /// GET /api/SupportTickets — List all support tickets (admin).
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<SupportTicketResponseDto>>> GetAll()
        {
            var tenant = GetTenant();
            await using var db = _dbFactory.CreateDbContext(tenant);

            var tickets = await db.SupportTickets
                .Include(t => t.Attachments)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new SupportTicketResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Urgency = t.Urgency,
                    Category = t.Category,
                    CurrentPage = t.CurrentPage,
                    RelatedUrl = t.RelatedUrl,
                    Tenant = t.Tenant,
                    UserEmail = t.UserEmail,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    Attachments = t.Attachments.Select(a => new SupportTicketAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileSize = a.FileSize,
                        ContentType = a.ContentType
                    }).ToList()
                })
                .ToListAsync();

            return Ok(tickets);
        }

        /// <summary>
        /// GET /api/SupportTickets/{id} — Get a single ticket.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<SupportTicketResponseDto>> GetById(int id)
        {
            var tenant = GetTenant();
            await using var db = _dbFactory.CreateDbContext(tenant);

            var ticket = await BuildResponseDto(db, id);
            if (ticket == null) return NotFound(new { error = $"Ticket {id} not found" });

            return Ok(ticket);
        }

        /// <summary>
        /// PATCH /api/SupportTickets/{id}/status — Update ticket status.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize]
        public async Task<ActionResult<SupportTicketResponseDto>> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var validStatuses = new[] { "open", "in_progress", "resolved", "closed" };
            if (string.IsNullOrWhiteSpace(dto.Status) || !validStatuses.Contains(dto.Status.ToLower()))
                return BadRequest(new { error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}" });

            var tenant = GetTenant();
            await using var db = _dbFactory.CreateDbContext(tenant);

            var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound(new { error = $"Ticket {id} not found" });

            ticket.Status = dto.Status.ToLower();
            await db.SaveChangesAsync();

            var response = await BuildResponseDto(db, id);
            return Ok(response);
        }

        private static async Task<SupportTicketResponseDto?> BuildResponseDto(ApplicationDbContext db, int ticketId)
        {
            return await db.SupportTickets
                .Include(t => t.Attachments)
                .Where(t => t.Id == ticketId)
                .Select(t => new SupportTicketResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Urgency = t.Urgency,
                    Category = t.Category,
                    CurrentPage = t.CurrentPage,
                    RelatedUrl = t.RelatedUrl,
                    Tenant = t.Tenant,
                    UserEmail = t.UserEmail,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    Attachments = t.Attachments.Select(a => new SupportTicketAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileSize = a.FileSize,
                        ContentType = a.ContentType
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}
