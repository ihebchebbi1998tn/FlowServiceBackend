using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Modules.Users.Models;

namespace MyApi.Modules.Skills.Models
{
    [Table("Skills")]
    public class Skill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Level { get; set; } // beginner, intermediate, advanced, expert

        [Required]
        [MaxLength(100)]
        public string CreatedUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ModifyUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    }
}
