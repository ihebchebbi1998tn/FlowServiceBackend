using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs
{
    // Response DTOs
    public class ContactResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public bool Favorite { get; set; }
        public DateTime? LastContactDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public List<ContactTagDto> Tags { get; set; } = new List<ContactTagDto>();
        public List<ContactNoteDto> Notes { get; set; } = new List<ContactNoteDto>();
    }

    public class ContactListResponseDto
    {
        public List<ContactResponseDto> Contacts { get; set; } = new List<ContactResponseDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Request DTOs
    public class CreateContactRequestDto
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Company { get; set; }

        [StringLength(255)]
        public string? Position { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "active";

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "individual";

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        public bool Favorite { get; set; } = false;

        public DateTime? LastContactDate { get; set; }

        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class UpdateContactRequestDto
    {
        [StringLength(255)]
        public string? Name { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Company { get; set; }

        [StringLength(255)]
        public string? Position { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        public bool? Favorite { get; set; }

        public DateTime? LastContactDate { get; set; }

        public List<int>? TagIds { get; set; }
    }

    public class ContactSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public List<int>? TagIds { get; set; }
        public bool? Favorite { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortDirection { get; set; } = "desc";
    }

    public class BulkImportContactRequestDto
    {
        public List<CreateContactRequestDto> Contacts { get; set; } = new List<CreateContactRequestDto>();
        public bool SkipDuplicates { get; set; } = true;
        public bool UpdateExisting { get; set; } = false;
    }

    public class BulkImportResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<ContactResponseDto> ImportedContacts { get; set; } = new List<ContactResponseDto>();
    }
}