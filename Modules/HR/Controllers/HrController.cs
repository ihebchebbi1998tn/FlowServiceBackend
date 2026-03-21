using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.HR.DTOs;
using MyApi.Modules.HR.Services;

namespace MyApi.Modules.HR.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/hr")]
    public class HrController : ControllerBase
    {
        private readonly IHrService _hrService;

        public HrController(IHrService hrService)
        {
            _hrService = hrService;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var data = await _hrService.GetEmployeesAsync();
            return Ok(new { success = true, data });
        }

        [HttpGet("employees/{id:int}")]
        public async Task<IActionResult> GetEmployeeDetail(int id)
        {
            var data = await _hrService.GetEmployeeDetailAsync(id);
            return Ok(new { success = true, data });
        }

        [HttpPut("employees/{userId:int}/salary-config")]
        public async Task<IActionResult> UpsertSalaryConfig(int userId, [FromBody] UpsertSalaryConfigDto dto)
        {
            var data = await _hrService.UpsertSalaryConfigAsync(userId, dto);
            return Ok(new { success = true, data });
        }

        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendance([FromQuery] int month, [FromQuery] int year, [FromQuery] int? userId = null)
        {
            var data = await _hrService.GetAttendanceAsync(month, year, userId);
            return Ok(new { success = true, data });
        }

        [HttpPost("attendance")]
        public async Task<IActionResult> CreateAttendance([FromBody] UpsertAttendanceDto dto)
        {
            var data = await _hrService.CreateAttendanceAsync(dto);
            return Ok(new { success = true, data });
        }

        [HttpPut("attendance/{id:int}")]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpsertAttendanceDto dto)
        {
            var data = await _hrService.UpdateAttendanceAsync(id, dto);
            return Ok(new { success = true, data });
        }

        [HttpPost("attendance/import")]
        public async Task<IActionResult> ImportAttendance([FromBody] ImportAttendanceDto dto)
        {
            var data = await _hrService.ImportAttendanceAsync(dto);
            return Ok(new { success = true, data });
        }

        [HttpGet("attendance/settings")]
        public async Task<IActionResult> GetAttendanceSettings()
        {
            var data = await _hrService.GetAttendanceSettingsAsync();
            return Ok(new { success = true, data });
        }

        [HttpPut("attendance/settings")]
        public async Task<IActionResult> UpdateAttendanceSettings([FromBody] UpsertAttendanceSettingsDto dto)
        {
            var data = await _hrService.UpdateAttendanceSettingsAsync(dto);
            return Ok(new { success = true, data });
        }

        [HttpGet("leaves/balances")]
        public async Task<IActionResult> GetLeaveBalances([FromQuery] int year)
        {
            var data = await _hrService.GetLeaveBalancesAsync(year);
            return Ok(new { success = true, data });
        }

        [HttpPut("leaves/balances/{userId:int}")]
        public async Task<IActionResult> SetLeaveAllowance(int userId, [FromBody] SetLeaveAllowanceDto dto)
        {
            var data = await _hrService.SetLeaveAllowanceAsync(userId, dto);
            return Ok(new { success = true, data });
        }

        [HttpPost("payroll/run")]
        public async Task<IActionResult> GeneratePayrollRun([FromBody] CreatePayrollRunDto dto)
        {
            var data = await _hrService.GeneratePayrollRunAsync(dto, GetCurrentUserId());
            return Ok(new { success = true, data });
        }

        [HttpGet("payroll/runs")]
        public async Task<IActionResult> ListPayrollRuns([FromQuery] int year)
        {
            var data = await _hrService.ListPayrollRunsAsync(year);
            return Ok(new { success = true, data });
        }

        [HttpGet("payroll/runs/{id:int}")]
        public async Task<IActionResult> GetPayrollRun(int id)
        {
            var data = await _hrService.GetPayrollRunAsync(id);
            return Ok(new { success = true, data });
        }

        [HttpPut("payroll/runs/{id:int}/confirm")]
        public async Task<IActionResult> ConfirmPayrollRun(int id)
        {
            var data = await _hrService.ConfirmPayrollRunAsync(id);
            return Ok(new { success = true, data });
        }

        [HttpGet("payroll/payslip/{entryId:int}")]
        public async Task<IActionResult> GetPayslip(int entryId)
        {
            var data = await _hrService.GetPayslipAsync(entryId);
            return Ok(new { success = true, data });
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var data = await _hrService.GetDepartmentsAsync();
            return Ok(new { success = true, data });
        }

        [HttpPost("departments")]
        public async Task<IActionResult> CreateDepartment([FromBody] UpsertDepartmentDto dto)
        {
            var data = await _hrService.CreateDepartmentAsync(dto);
            return Ok(new { success = true, data });
        }

        [HttpPut("departments/{id:int}")]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpsertDepartmentDto dto)
        {
            var data = await _hrService.UpdateDepartmentAsync(id, dto);
            return Ok(new { success = true, data });
        }

        [HttpDelete("departments/{id:int}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            await _hrService.DeleteDepartmentAsync(id);
            return Ok(new { success = true });
        }

        private int GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("UserId")
                ?? "0";
            return int.TryParse(raw, out var userId) ? userId : 0;
        }
    }
}
