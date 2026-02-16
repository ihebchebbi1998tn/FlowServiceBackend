using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Dispatches.Models
{
    [Table("DispatchTechnicians")]
    public class DispatchTechnician
    {
        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("DispatchId")]
        public int DispatchId { get; set; }

        [Required]
        [Column("TechnicianId")]
        public int TechnicianId { get; set; }

        [Column("AssignedDate")]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Column("Role")]
        [MaxLength(50)]
        public string? Role { get; set; }
    }
}
