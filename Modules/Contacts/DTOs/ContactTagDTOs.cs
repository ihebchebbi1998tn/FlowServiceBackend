using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Contacts.DTOs
{
    public class ContactTagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ContactCount { get; set; } = 0;
    }

    public class CreateContactTagRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Color { get; set; } = "#3b82f6";

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateContactTagRequestDto
    {
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class ContactTagListResponseDto
    {
        public List<ContactTagDto> Tags { get; set; } = new List<ContactTagDto>();
        public int TotalCount { get; set; }
    }

    public class AssignTagToContactRequestDto
    {
        [Required]
        public int ContactId { get; set; }

        [Required]
        public int TagId { get; set; }
    }
}
