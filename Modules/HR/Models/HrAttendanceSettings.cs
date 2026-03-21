using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Infrastructure;

namespace MyApi.Modules.HR.Models
{
    [Table("hr_attendance_settings")]
    public class HrAttendanceSettings : ITenantEntity
    {
        public int TenantId { get; set; }

        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("weekend_days")]
        public string WeekendDays { get; set; } = "[0,6]";

        [Column("standard_hours_per_day")]
        public decimal StandardHoursPerDay { get; set; } = 8;

        [Column("overtime_threshold")]
        public decimal OvertimeThreshold { get; set; } = 8;

        [Column("overtime_multiplier")]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;

        [Column("rounding_method")]
        [MaxLength(20)]
        public string RoundingMethod { get; set; } = "none";

        [Column("calculation_method")]
        [MaxLength(30)]
        public string CalculationMethod { get; set; } = "actual_hours";

        [Column("late_threshold_minutes")]
        public int LateThresholdMinutes { get; set; } = 10;

        [Column("holidays")]
        public string Holidays { get; set; } = "[]";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
