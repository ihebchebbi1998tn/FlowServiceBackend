using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Roles.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        public bool Granted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; } = "system";

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation property
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }
}
