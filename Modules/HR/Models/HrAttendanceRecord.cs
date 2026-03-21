using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Infrastructure;

namespace MyApi.Modules.HR.Models
{
    [Table("hr_attendance_records")]
    public class HrAttendanceRecord : ITenantEntity
    {
        public int TenantId { get; set; }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("check_in")]
        public TimeSpan? CheckIn { get; set; }

        [Column("check_out")]
        public TimeSpan? CheckOut { get; set; }

        [Column("break_duration")]
        public int? BreakDuration { get; set; }

        [Column("source")]
        [MaxLength(30)]
        public string Source { get; set; } = "manual";

        [Column("raw_data")]
        public string? RawData { get; set; }

        [Column("hours_worked")]
        public decimal? HoursWorked { get; set; }

        [Column("overtime_hours")]
        public decimal? OvertimeHours { get; set; }

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "present";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
