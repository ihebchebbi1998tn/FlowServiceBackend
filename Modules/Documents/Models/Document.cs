using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Documents.Models
{
    [Table("Documents")]
    public class Document
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("FileName")]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Column("OriginalName")]
        [MaxLength(500)]
        public string OriginalName { get; set; } = string.Empty;

        [Required]
        [Column("FilePath")]
        [MaxLength(1000)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [Column("FileSize")]
        public long FileSize { get; set; }

        [Required]
        [Column("ContentType")]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Column("ModuleType")]
        [MaxLength(50)]
        public string? ModuleType { get; set; }

        [Column("ModuleId")]
        [MaxLength(100)]
        public string? ModuleId { get; set; }

        [Column("ModuleName")]
        [MaxLength(255)]
        public string? ModuleName { get; set; }

        [Column("Category")]
        [MaxLength(50)]
        public string Category { get; set; } = "crm";

        [Column("Description")]
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column("Tags")]
        [MaxLength(500)]
        public string? Tags { get; set; }

        [Column("IsPublic")]
        public bool IsPublic { get; set; } = false;

        [Required]
        [Column("UploadedBy")]
        [MaxLength(100)]
        public string UploadedBy { get; set; } = string.Empty;

        [Column("UploadedByName")]
        [MaxLength(200)]
        public string? UploadedByName { get; set; }

        [Required]
        [Column("UploadedAt")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Original file size before compression (bytes)
        /// </summary>
        [Column("OriginalFileSize")]
        public long? OriginalFileSize { get; set; }

        /// <summary>
        /// Whether the file is compressed on disk
        /// </summary>
        [Column("IsCompressed")]
        public bool IsCompressed { get; set; } = false;

        /// <summary>
        /// Compression ratio: (OriginalFileSize - FileSize) / OriginalFileSize * 100
        /// </summary>
        [Column("CompressionRatio")]
        public decimal? CompressionRatio { get; set; }

        /// <summary>
        /// Compression method used (e.g., "gzip", "deflate", "none")
        /// </summary>
        [Column("CompressionMethod")]
        [MaxLength(50)]
        public string CompressionMethod { get; set; } = "none";
    }
}
