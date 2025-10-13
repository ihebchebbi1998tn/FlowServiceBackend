using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Articles.Models
{
    [Table("locations")]
    public class Location
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // 'warehouse', 'vehicle', 'office', 'other'

        [Column("address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Column("assigned_technician")]
        [MaxLength(50)]
        public string? AssignedTechnician { get; set; } // For mobile locations like vehicles

        [Column("capacity")]
        public int? Capacity { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
