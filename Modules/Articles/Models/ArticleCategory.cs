using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Articles.Models
{
    [Table("article_categories")]
    public class ArticleCategory
    {
        [Key]
        [Column("id")]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // 'material', 'service', or 'both'

        [Column("description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Column("parent_id")]
        [MaxLength(50)]
        public string? ParentId { get; set; }

        [Required]
        [Column("order")]
        public int Order { get; set; }

        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
