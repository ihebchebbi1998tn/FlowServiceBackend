using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.HR.DTOs
{
    public class UpsertSalaryConfigDto
    {
        public decimal? GrossSalary { get; set; }
        public bool? IsHeadOfFamily { get; set; }
        public int? ChildrenCount { get; set; }
        public decimal? CustomDeductions { get; set; }
        public string? BankAccount { get; set; }
        public string? CnssNumber { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? EmploymentType { get; set; }
        public string? Cin { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    public class HrEmployeeSalaryConfigDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal GrossSalary { get; set; }
        public bool IsHeadOfFamily { get; set; }
        public int ChildrenCount { get; set; }
        public decimal? CustomDeductions { get; set; }
        public string? BankAccount { get; set; }
        public string? CnssNumber { get; set; }
        public DateTime? HireDate { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string EmploymentType { get; set; } = "full_time";
        public string? Cin { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? MaritalStatus { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }

    public class HrAttendanceDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public int? BreakDuration { get; set; }
        public string Source { get; set; } = "manual";
        public object? RawData { get; set; }
        public decimal? HoursWorked { get; set; }
        public decimal? OvertimeHours { get; set; }
        public string Status { get; set; } = "present";
        public string? Notes { get; set; }
    }

    public class UpsertAttendanceDto
    {
        public int? UserId { get; set; }
        public DateTime? Date { get; set; }
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public int? BreakDuration { get; set; }
        public string? Source { get; set; }
        public object? RawData { get; set; }
        public decimal? HoursWorked { get; set; }
        public decimal? OvertimeHours { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class ImportAttendanceDto
    {
        public List<UpsertAttendanceDto> Rows { get; set; } = new();
    }

    public class HrAttendanceSettingsDto
    {
        public int Id { get; set; }
        public List<int> WeekendDays { get; set; } = new();
        public decimal StandardHoursPerDay { get; set; }
        public decimal OvertimeThreshold { get; set; }
        public decimal OvertimeMultiplier { get; set; }
        public string RoundingMethod { get; set; } = "none";
        public string CalculationMethod { get; set; } = "actual_hours";
        public int LateThresholdMinutes { get; set; }
        public List<string> Holidays { get; set; } = new();
    }

    public class UpsertAttendanceSettingsDto
    {
        public List<int>? WeekendDays { get; set; }
        public decimal? StandardHoursPerDay { get; set; }
        public decimal? OvertimeThreshold { get; set; }
        public decimal? OvertimeMultiplier { get; set; }
        public string? RoundingMethod { get; set; }
        public string? CalculationMethod { get; set; }
        public int? LateThresholdMinutes { get; set; }
        public List<string>? Holidays { get; set; }
    }

    public class HrLeaveBalanceDto
    {
        public int UserId { get; set; }
        public string LeaveType { get; set; } = "annual";
        public decimal AnnualAllowance { get; set; }
        public decimal Used { get; set; }
        public decimal Remaining { get; set; }
        public decimal Pending { get; set; }
    }

    public class SetLeaveAllowanceDto
    {
        [Required]
        public int Year { get; set; }
        [Required]
        public string LeaveType { get; set; } = "annual";
        [Required]
        public decimal AnnualAllowance { get; set; }
    }

    public class CreatePayrollRunDto
    {
        [Required]
        public int Month { get; set; }
        [Required]
        public int Year { get; set; }
    }

    public class HrPayrollEntryDto
    {
        public int Id { get; set; }
        public int PayrollRunId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public decimal GrossSalary { get; set; }
        public decimal Cnss { get; set; }
        public decimal TaxableGross { get; set; }
        public decimal Abattement { get; set; }
        public decimal TaxableBase { get; set; }
        public decimal Irpp { get; set; }
        public decimal Css { get; set; }
        public decimal NetSalary { get; set; }
        public decimal WorkedDays { get; set; }
        public decimal TotalHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal LeaveDays { get; set; }
        public object? Details { get; set; }
    }

    public class HrPayrollRunDto
    {
        public int Id { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Status { get; set; } = "draft";
        public List<HrPayrollEntryDto> Entries { get; set; } = new();
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    public class HrDepartmentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public int? ManagerId { get; set; }
        public string? Description { get; set; }
        public int? Position { get; set; }
    }

    public class UpsertDepartmentDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public int? ManagerId { get; set; }
        public string? Description { get; set; }
        public int? Position { get; set; }
    }
}
