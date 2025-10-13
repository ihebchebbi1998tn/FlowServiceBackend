using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Contacts.Models
{
    public class Contact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Company { get; set; }

        [StringLength(255)]
        public string? Position { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, prospect, customer

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "individual"; // individual, company

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        public bool Favorite { get; set; } = false;

        public DateTime? LastContactDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual ICollection<ContactNote> Notes { get; set; } = new List<ContactNote>();
        public virtual ICollection<ContactTagAssignment> TagAssignments { get; set; } = new List<ContactTagAssignment>();
    }
}
