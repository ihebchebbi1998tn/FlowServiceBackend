using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApi.Modules.HR.DTOs;

namespace MyApi.Modules.HR.Services
{
    public interface IHrService
    {
        // ---- Employees & Salary ----
        Task<List<object>> GetEmployeesAsync();
        Task<object> GetEmployeeDetailAsync(int userId);
        Task<HrEmployeeSalaryConfigDto> UpsertSalaryConfigAsync(int userId, UpsertSalaryConfigDto dto, int actorUserId);
        Task<List<HrSalaryHistoryDto>> GetSalaryHistoryAsync(int userId);

        // ---- Leaves (HR is source of truth; Planning consumes via API) ----
        Task<List<HrLeaveBalanceDto>> GetLeaveBalancesAsync(int year);
        Task<List<HrLeaveBalanceDto>> SetLeaveAllowanceAsync(int userId, SetLeaveAllowanceDto dto);

        // ---- Payroll ----
        Task<HrPayrollRunDto> GeneratePayrollRunAsync(CreatePayrollRunDto dto, int createdByUserId);
        Task<List<HrPayrollRunDto>> ListPayrollRunsAsync(int year);
        Task<HrPayrollRunDto> GetPayrollRunAsync(int id);
        Task<HrPayrollRunDto> ConfirmPayrollRunAsync(int id, int actorUserId);
        Task<object> GetPayslipAsync(int entryId);

        // ---- Departments ----
        Task<List<HrDepartmentDto>> GetDepartmentsAsync();
        Task<HrDepartmentDto> CreateDepartmentAsync(UpsertDepartmentDto dto);
        Task<HrDepartmentDto> UpdateDepartmentAsync(int id, UpsertDepartmentDto dto);
        Task DeleteDepartmentAsync(int id);

        // ---- Bonuses & Costs ----
        Task<List<HrBonusCostDto>> GetBonusCostsAsync(int? userId, int? year, int? month, string? kind);
        Task<HrBonusCostDto> CreateBonusCostAsync(UpsertBonusCostDto dto, int actorUserId);
        Task<HrBonusCostDto> UpdateBonusCostAsync(int id, UpsertBonusCostDto dto, int actorUserId);
        Task DeleteBonusCostAsync(int id, int actorUserId);

        // ---- CNSS Rates ----
        Task<List<HrCnssRateDto>> GetCnssRatesAsync();
        Task<HrCnssRateDto> GetActiveCnssRateAsync();
        Task<HrCnssRateDto> UpsertCnssRateAsync(UpsertCnssRateDto dto, int actorUserId);

        // ---- Public Holidays ----
        Task<List<HrPublicHolidayDto>> GetPublicHolidaysAsync(int? year);
        Task<HrPublicHolidayDto> CreatePublicHolidayAsync(UpsertPublicHolidayDto dto);
        Task<HrPublicHolidayDto> UpdatePublicHolidayAsync(int id, UpsertPublicHolidayDto dto);
        Task DeletePublicHolidayAsync(int id);

        // ---- Employee Documents ----
        Task<List<HrEmployeeDocumentDto>> GetEmployeeDocumentsAsync(int userId);
        Task<HrEmployeeDocumentDto> CreateEmployeeDocumentAsync(UpsertEmployeeDocumentDto dto, int actorUserId);
        Task DeleteEmployeeDocumentAsync(int id);

        // ---- Audit Log ----
        Task<List<HrAuditLogDto>> GetAuditLogAsync(int? userId, int take = 100);

        // ---- Reports ----
        Task<List<HrEmployeeCostDto>> GetEmployeeCostReportAsync(int year, int? month);
        Task<HrCnssMonthlyDeclarationDto> GetCnssDeclarationAsync(int year, int month);
    }
}
