using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Infrastructure;

namespace MyApi.Modules.Purchases.Models
{
    [Table("GoodsReceipts")]
    public class GoodsReceipt : ITenantEntity
    {
        public int TenantId { get; set; }

        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("ReceiptNumber")]
        [MaxLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [Column("PurchaseOrderId")]
        public int PurchaseOrderId { get; set; }

        [Required]
        [Column("SupplierId")]
        public int SupplierId { get; set; }

        [Column("SupplierName")]
        [MaxLength(255)]
        public string SupplierName { get; set; } = string.Empty;

        [Required]
        [Column("ReceiptDate")]
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("Status")]
        [MaxLength(20)]
        public string Status { get; set; } = "partial";
        // partial, complete, rejected

        [Column("DeliveryNoteRef")]
        [MaxLength(100)]
        public string? DeliveryNoteRef { get; set; }

        [Column("Notes")]
        public string? Notes { get; set; }

        [Required]
        [Column("ReceivedBy")]
        [MaxLength(100)]
        public string ReceivedBy { get; set; } = string.Empty;

        [Column("ReceivedByName")]
        [MaxLength(255)]
        public string? ReceivedByName { get; set; }

        [Required]
        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("CreatedBy")]
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("ModifiedDate")]
        public DateTime? ModifiedDate { get; set; }

        [Column("ModifiedBy")]
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation
        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("SupplierId")]
        public virtual MyApi.Modules.Contacts.Models.Contact? Supplier { get; set; }

        public virtual ICollection<GoodsReceiptItem>? Items { get; set; }
    }
}
