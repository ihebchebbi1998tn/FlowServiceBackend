using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Contacts.Models
{
    public class ContactNote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        // Navigation property
        [ForeignKey("ContactId")]
        public virtual Contact Contact { get; set; } = null!;
    }
}
