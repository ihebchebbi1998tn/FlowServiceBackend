using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Contacts.Models
{
    public class ContactTagAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }

        [Required]
        public int TagId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? AssignedBy { get; set; }

        // Navigation properties
        [ForeignKey("ContactId")]
        public virtual Contact Contact { get; set; } = null!;

        [ForeignKey("TagId")]
        public virtual ContactTag Tag { get; set; } = null!;
    }
}
