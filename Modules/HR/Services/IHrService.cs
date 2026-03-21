using System.Collections.Generic;
using System.Threading.Tasks;
using MyApi.Modules.HR.DTOs;

namespace MyApi.Modules.HR.Services
{
    public interface IHrService
    {
        Task<List<object>> GetEmployeesAsync();
        Task<object> GetEmployeeDetailAsync(int userId);
        Task<HrEmployeeSalaryConfigDto> UpsertSalaryConfigAsync(int userId, UpsertSalaryConfigDto dto);

        Task<List<HrAttendanceDto>> GetAttendanceAsync(int month, int year, int? userId);
        Task<HrAttendanceDto> CreateAttendanceAsync(UpsertAttendanceDto dto);
        Task<HrAttendanceDto> UpdateAttendanceAsync(int id, UpsertAttendanceDto dto);
        Task<object> ImportAttendanceAsync(ImportAttendanceDto dto);
        Task<HrAttendanceSettingsDto> GetAttendanceSettingsAsync();
        Task<HrAttendanceSettingsDto> UpdateAttendanceSettingsAsync(UpsertAttendanceSettingsDto dto);

        Task<List<HrLeaveBalanceDto>> GetLeaveBalancesAsync(int year);
        Task<List<HrLeaveBalanceDto>> SetLeaveAllowanceAsync(int userId, SetLeaveAllowanceDto dto);

        Task<HrPayrollRunDto> GeneratePayrollRunAsync(CreatePayrollRunDto dto, int createdByUserId);
        Task<List<HrPayrollRunDto>> ListPayrollRunsAsync(int year);
        Task<HrPayrollRunDto> GetPayrollRunAsync(int id);
        Task<HrPayrollRunDto> ConfirmPayrollRunAsync(int id);
        Task<object> GetPayslipAsync(int entryId);

        Task<List<HrDepartmentDto>> GetDepartmentsAsync();
        Task<HrDepartmentDto> CreateDepartmentAsync(UpsertDepartmentDto dto);
        Task<HrDepartmentDto> UpdateDepartmentAsync(int id, UpsertDepartmentDto dto);
        Task DeleteDepartmentAsync(int id);
    }
}
