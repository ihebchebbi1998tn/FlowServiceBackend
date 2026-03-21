using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.HR.DTOs;
using MyApi.Modules.HR.Models;
using MyApi.Modules.Planning.Models;

namespace MyApi.Modules.HR.Services
{
    public class HrService : IHrService
    {
        private readonly ApplicationDbContext _db;

        public HrService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<object>> GetEmployeesAsync()
        {
            var users = await _db.Users
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var salaryConfigs = await _db.Set<HrEmployeeSalaryConfig>()
                .Where(x => userIds.Contains(x.UserId))
                .ToListAsync();

            return users.Select(u =>
            {
                var salaryConfig = salaryConfigs.FirstOrDefault(s => s.UserId == u.Id);
                return new
                {
                    user = MapSafeUser(u),
                    salaryConfig = salaryConfig != null ? MapSalaryConfigDto(salaryConfig) : null
                };
            }).Cast<object>().ToList();
        }

        public async Task<object> GetEmployeeDetailAsync(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) throw new KeyNotFoundException("Employee not found");

            var salaryConfig = await _db.Set<HrEmployeeSalaryConfig>().FirstOrDefaultAsync(x => x.UserId == userId);
            var recentAttendance = await _db.Set<HrAttendanceRecord>()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Date)
                .Take(31)
                .ToListAsync();
            var leaves = await _db.Set<UserLeave>()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.StartDate)
                .Take(20)
                .ToListAsync();

            return new
            {
                user = MapSafeUser(user),
                salaryConfig = salaryConfig != null ? MapSalaryConfigDto(salaryConfig) : null,
                attendance = recentAttendance.Select(MapAttendanceDto).ToList(),
                leaves
            };
        }

        public async Task<HrEmployeeSalaryConfigDto> UpsertSalaryConfigAsync(int userId, UpsertSalaryConfigDto dto)
        {
            var entity = await _db.Set<HrEmployeeSalaryConfig>().FirstOrDefaultAsync(x => x.UserId == userId);
            if (entity == null)
            {
                entity = new HrEmployeeSalaryConfig
                {
                    UserId = userId
                };
                _db.Set<HrEmployeeSalaryConfig>().Add(entity);
            }

            entity.GrossSalary = dto.GrossSalary ?? entity.GrossSalary;
            entity.IsHeadOfFamily = dto.IsHeadOfFamily ?? entity.IsHeadOfFamily;
            entity.ChildrenCount = dto.ChildrenCount ?? entity.ChildrenCount;
            entity.CustomDeductions = dto.CustomDeductions ?? entity.CustomDeductions;
            entity.BankAccount = dto.BankAccount ?? entity.BankAccount;
            entity.CnssNumber = dto.CnssNumber ?? entity.CnssNumber;
            entity.HireDate = dto.HireDate ?? entity.HireDate;
            entity.Department = dto.Department ?? entity.Department;
            entity.Position = dto.Position ?? entity.Position;
            entity.EmploymentType = dto.EmploymentType ?? entity.EmploymentType;
            entity.Cin = dto.Cin ?? entity.Cin;
            entity.BirthDate = dto.BirthDate ?? entity.BirthDate;
            entity.MaritalStatus = dto.MaritalStatus ?? entity.MaritalStatus;
            entity.AddressLine1 = dto.AddressLine1 ?? entity.AddressLine1;
            entity.AddressLine2 = dto.AddressLine2 ?? entity.AddressLine2;
            entity.City = dto.City ?? entity.City;
            entity.PostalCode = dto.PostalCode ?? entity.PostalCode;
            entity.EmergencyContactName = dto.EmergencyContactName ?? entity.EmergencyContactName;
            entity.EmergencyContactPhone = dto.EmergencyContactPhone ?? entity.EmergencyContactPhone;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapSalaryConfigDto(entity);
        }

        public async Task<List<HrAttendanceDto>> GetAttendanceAsync(int month, int year, int? userId)
        {
            var query = _db.Set<HrAttendanceRecord>()
                .Where(x => x.Date.Month == month && x.Date.Year == year);

            if (userId.HasValue)
            {
                query = query.Where(x => x.UserId == userId.Value);
            }

            var rows = await query.OrderBy(x => x.Date).ToListAsync();
            return rows.Select(MapAttendanceDto).ToList();
        }

        public async Task<HrAttendanceDto> CreateAttendanceAsync(UpsertAttendanceDto dto)
        {
            if (!dto.UserId.HasValue || !dto.Date.HasValue)
            {
                throw new InvalidOperationException("userId and date are required");
            }

            var existing = await _db.Set<HrAttendanceRecord>()
                .FirstOrDefaultAsync(x => x.UserId == dto.UserId.Value && x.Date == dto.Date.Value.Date);
            if (existing != null)
            {
                existing.CheckIn = ParseTime(dto.CheckIn);
                existing.CheckOut = ParseTime(dto.CheckOut);
                existing.BreakDuration = dto.BreakDuration;
                existing.Source = dto.Source ?? existing.Source;
                existing.RawData = dto.RawData != null ? JsonSerializer.Serialize(dto.RawData) : existing.RawData;
                existing.HoursWorked = dto.HoursWorked ?? existing.HoursWorked;
                existing.OvertimeHours = dto.OvertimeHours ?? existing.OvertimeHours;
                existing.Status = dto.Status ?? existing.Status;
                existing.Notes = dto.Notes ?? existing.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return MapAttendanceDto(existing);
            }
            else
            {
                var entity = new HrAttendanceRecord
                {
                    UserId = dto.UserId.Value,
                    Date = dto.Date.Value.Date,
                    CheckIn = ParseTime(dto.CheckIn),
                    CheckOut = ParseTime(dto.CheckOut),
                    BreakDuration = dto.BreakDuration,
                    Source = dto.Source ?? "manual",
                    RawData = dto.RawData != null ? JsonSerializer.Serialize(dto.RawData) : null,
                    HoursWorked = dto.HoursWorked,
                    OvertimeHours = dto.OvertimeHours,
                    Status = dto.Status ?? "present",
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Set<HrAttendanceRecord>().Add(entity);
                await _db.SaveChangesAsync();
                return MapAttendanceDto(entity);
            }
        }

        public async Task<HrAttendanceDto> UpdateAttendanceAsync(int id, UpsertAttendanceDto dto)
        {
            var entity = await _db.Set<HrAttendanceRecord>().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) throw new KeyNotFoundException("Attendance record not found");

            if (dto.UserId.HasValue) entity.UserId = dto.UserId.Value;
            if (dto.Date.HasValue) entity.Date = dto.Date.Value.Date;
            if (dto.CheckIn != null) entity.CheckIn = ParseTime(dto.CheckIn);
            if (dto.CheckOut != null) entity.CheckOut = ParseTime(dto.CheckOut);
            if (dto.BreakDuration.HasValue) entity.BreakDuration = dto.BreakDuration;
            entity.Source = dto.Source ?? entity.Source;
            entity.RawData = dto.RawData != null ? JsonSerializer.Serialize(dto.RawData) : entity.RawData;
            entity.HoursWorked = dto.HoursWorked ?? entity.HoursWorked;
            entity.OvertimeHours = dto.OvertimeHours ?? entity.OvertimeHours;
            entity.Status = dto.Status ?? entity.Status;
            entity.Notes = dto.Notes ?? entity.Notes;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapAttendanceDto(entity);
        }

        public async Task<object> ImportAttendanceAsync(ImportAttendanceDto dto)
        {
            var imported = 0;
            var errors = new List<object>();

            foreach (var row in dto.Rows)
            {
                try
                {
                    await CreateAttendanceAsync(row);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add(new { row.UserId, row.Date, error = ex.Message });
                }
            }

            return new { imported, errors };
        }

        public async Task<HrAttendanceSettingsDto> GetAttendanceSettingsAsync()
        {
            var entity = await _db.Set<HrAttendanceSettings>().FirstOrDefaultAsync();
            if (entity == null)
            {
                entity = new HrAttendanceSettings();
                _db.Set<HrAttendanceSettings>().Add(entity);
                await _db.SaveChangesAsync();
            }
            return MapAttendanceSettings(entity);
        }

        public async Task<HrAttendanceSettingsDto> UpdateAttendanceSettingsAsync(UpsertAttendanceSettingsDto dto)
        {
            var entity = await _db.Set<HrAttendanceSettings>().FirstOrDefaultAsync();
            if (entity == null)
            {
                entity = new HrAttendanceSettings();
                _db.Set<HrAttendanceSettings>().Add(entity);
            }

            if (dto.WeekendDays != null) entity.WeekendDays = JsonSerializer.Serialize(dto.WeekendDays);
            if (dto.StandardHoursPerDay.HasValue) entity.StandardHoursPerDay = dto.StandardHoursPerDay.Value;
            if (dto.OvertimeThreshold.HasValue) entity.OvertimeThreshold = dto.OvertimeThreshold.Value;
            if (dto.OvertimeMultiplier.HasValue) entity.OvertimeMultiplier = dto.OvertimeMultiplier.Value;
            if (!string.IsNullOrWhiteSpace(dto.RoundingMethod)) entity.RoundingMethod = dto.RoundingMethod;
            if (!string.IsNullOrWhiteSpace(dto.CalculationMethod)) entity.CalculationMethod = dto.CalculationMethod;
            if (dto.LateThresholdMinutes.HasValue) entity.LateThresholdMinutes = dto.LateThresholdMinutes.Value;
            if (dto.Holidays != null) entity.Holidays = JsonSerializer.Serialize(dto.Holidays);
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return MapAttendanceSettings(entity);
        }

        public async Task<List<HrLeaveBalanceDto>> GetLeaveBalancesAsync(int year)
        {
            var balances = await _db.Set<HrLeaveBalance>().Where(x => x.Year == year).ToListAsync();
            var leaves = await _db.Set<UserLeave>().Where(x => x.StartDate.Year == year || x.EndDate.Year == year).ToListAsync();

            foreach (var bal in balances)
            {
                var related = leaves.Where(x => x.UserId == bal.UserId && x.LeaveType == bal.LeaveType).ToList();
                bal.Used = related.Where(x => x.Status == "approved").Sum(x => (decimal)(x.EndDate.Date - x.StartDate.Date).TotalDays + 1);
                bal.Pending = related.Where(x => x.Status == "pending").Sum(x => (decimal)(x.EndDate.Date - x.StartDate.Date).TotalDays + 1);
                bal.Remaining = bal.AnnualAllowance - bal.Used - bal.Pending;
            }
            await _db.SaveChangesAsync();

            return balances.Select(x => new HrLeaveBalanceDto
            {
                UserId = x.UserId,
                LeaveType = x.LeaveType,
                AnnualAllowance = x.AnnualAllowance,
                Used = x.Used,
                Pending = x.Pending,
                Remaining = x.Remaining
            }).ToList();
        }

        public async Task<List<HrLeaveBalanceDto>> SetLeaveAllowanceAsync(int userId, SetLeaveAllowanceDto dto)
        {
            var balance = await _db.Set<HrLeaveBalance>()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Year == dto.Year && x.LeaveType == dto.LeaveType);
            if (balance == null)
            {
                balance = new HrLeaveBalance
                {
                    UserId = userId,
                    Year = dto.Year,
                    LeaveType = dto.LeaveType
                };
                _db.Set<HrLeaveBalance>().Add(balance);
            }

            balance.AnnualAllowance = dto.AnnualAllowance;
            balance.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await GetLeaveBalancesAsync(dto.Year);
        }

        public async Task<HrPayrollRunDto> GeneratePayrollRunAsync(CreatePayrollRunDto dto, int createdByUserId)
        {
            var run = new HrPayrollRun
            {
                Month = dto.Month,
                Year = dto.Year,
                Status = "draft",
                CreatedBy = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<HrPayrollRun>().Add(run);
            await _db.SaveChangesAsync();

            var users = await _db.Users.Where(u => u.IsActive && !u.IsDeleted).ToListAsync();
            var salaryConfigs = await _db.Set<HrEmployeeSalaryConfig>().Where(x => users.Select(u => u.Id).Contains(x.UserId)).ToListAsync();
            var attendances = await _db.Set<HrAttendanceRecord>()
                .Where(x => x.Date.Month == dto.Month && x.Date.Year == dto.Year)
                .ToListAsync();
            var leaves = await _db.Set<UserLeave>()
                .Where(x => (x.StartDate.Month == dto.Month && x.StartDate.Year == dto.Year) || (x.EndDate.Month == dto.Month && x.EndDate.Year == dto.Year))
                .ToListAsync();

            foreach (var user in users)
            {
                var config = salaryConfigs.FirstOrDefault(x => x.UserId == user.Id);
                var gross = config?.GrossSalary ?? 0;
                var overtimeHours = attendances.Where(x => x.UserId == user.Id).Sum(x => x.OvertimeHours ?? 0);
                var totalHours = attendances.Where(x => x.UserId == user.Id).Sum(x => x.HoursWorked ?? 0);
                var leaveDays = leaves.Where(x => x.UserId == user.Id && x.Status == "approved")
                    .Sum(x => (decimal)(x.EndDate.Date - x.StartDate.Date).TotalDays + 1);

                var cnss = Math.Round(gross * 0.0918m, 3);
                var taxableGross = gross - cnss;
                var abattement = Math.Round((config?.IsHeadOfFamily ?? false ? 300m : 0m) + (config?.ChildrenCount ?? 0) * 100m, 3);
                var taxableBase = Math.Max(0, taxableGross - abattement);
                var irpp = Math.Round(taxableBase * 0.15m, 3);
                var css = Math.Round(gross * 0.01m, 3);
                var net = gross - cnss - irpp - css - (config?.CustomDeductions ?? 0m);

                _db.Set<HrPayrollEntry>().Add(new HrPayrollEntry
                {
                    PayrollRunId = run.Id,
                    UserId = user.Id,
                    GrossSalary = gross,
                    Cnss = cnss,
                    TaxableGross = taxableGross,
                    Abattement = abattement,
                    TaxableBase = taxableBase,
                    Irpp = irpp,
                    Css = css,
                    NetSalary = Math.Round(net, 3),
                    WorkedDays = Math.Round(totalHours / 8m, 2),
                    TotalHours = totalHours,
                    OvertimeHours = overtimeHours,
                    LeaveDays = leaveDays,
                    Details = JsonSerializer.Serialize(new
                    {
                        formula = "default_tn_v1",
                        customDeductions = config?.CustomDeductions ?? 0m
                    }),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return await GetPayrollRunAsync(run.Id);
        }

        public async Task<List<HrPayrollRunDto>> ListPayrollRunsAsync(int year)
        {
            var runs = await _db.Set<HrPayrollRun>()
                .Where(x => x.Year == year)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();

            var runIds = runs.Select(x => x.Id).ToList();
            var entries = await _db.Set<HrPayrollEntry>().Where(x => runIds.Contains(x.PayrollRunId)).ToListAsync();
            var userIds = entries.Select(e => e.UserId).Distinct().ToList();
            var userMap = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

            return runs.Select(run =>
            {
                run.TotalGross = entries.Where(e => e.PayrollRunId == run.Id).Sum(e => e.GrossSalary);
                run.TotalNet = entries.Where(e => e.PayrollRunId == run.Id).Sum(e => e.NetSalary);
                return MapPayrollRunDto(run, entries.Where(e => e.PayrollRunId == run.Id).ToList(), userMap);
            }).ToList();
        }

        public async Task<HrPayrollRunDto> GetPayrollRunAsync(int id)
        {
            var run = await _db.Set<HrPayrollRun>().FirstOrDefaultAsync(x => x.Id == id);
            if (run == null) throw new KeyNotFoundException("Payroll run not found");

            var entries = await _db.Set<HrPayrollEntry>().Where(x => x.PayrollRunId == id).ToListAsync();
            var userMap = await _db.Users
                .Where(u => entries.Select(e => e.UserId).Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

            run.TotalGross = entries.Sum(x => x.GrossSalary);
            run.TotalNet = entries.Sum(x => x.NetSalary);
            await _db.SaveChangesAsync();

            return MapPayrollRunDto(run, entries, userMap);
        }

        public async Task<HrPayrollRunDto> ConfirmPayrollRunAsync(int id)
        {
            var run = await _db.Set<HrPayrollRun>().FirstOrDefaultAsync(x => x.Id == id);
            if (run == null) throw new KeyNotFoundException("Payroll run not found");
            run.Status = "confirmed";
            run.ConfirmedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return await GetPayrollRunAsync(id);
        }

        public async Task<object> GetPayslipAsync(int entryId)
        {
            var entry = await _db.Set<HrPayrollEntry>().FirstOrDefaultAsync(x => x.Id == entryId);
            if (entry == null) throw new KeyNotFoundException("Payroll entry not found");

            var run = await _db.Set<HrPayrollRun>().FirstOrDefaultAsync(x => x.Id == entry.PayrollRunId);
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == entry.UserId);

            return new
            {
                entryId = entry.Id,
                payrollRunId = entry.PayrollRunId,
                month = run?.Month,
                year = run?.Year,
                userId = entry.UserId,
                userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : string.Empty,
                breakdown = new
                {
                    entry.GrossSalary,
                    entry.Cnss,
                    entry.TaxableGross,
                    entry.Abattement,
                    entry.TaxableBase,
                    entry.Irpp,
                    entry.Css,
                    entry.NetSalary,
                    entry.WorkedDays,
                    entry.TotalHours,
                    entry.OvertimeHours,
                    entry.LeaveDays
                },
                details = ParseJsonObject(entry.Details)
            };
        }

        public async Task<List<HrDepartmentDto>> GetDepartmentsAsync()
        {
            var rows = await _db.Set<HrDepartment>()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Position ?? int.MaxValue)
                .ThenBy(x => x.Name)
                .ToListAsync();
            return rows.Select(MapDepartmentDto).ToList();
        }

        public async Task<HrDepartmentDto> CreateDepartmentAsync(UpsertDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Department name is required");
            }

            var row = new HrDepartment
            {
                Name = dto.Name.Trim(),
                Code = dto.Code,
                ParentId = dto.ParentId,
                ManagerId = dto.ManagerId,
                Description = dto.Description,
                Position = dto.Position,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Set<HrDepartment>().Add(row);
            await _db.SaveChangesAsync();
            return MapDepartmentDto(row);
        }

        public async Task<HrDepartmentDto> UpdateDepartmentAsync(int id, UpsertDepartmentDto dto)
        {
            var row = await _db.Set<HrDepartment>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (row == null) throw new KeyNotFoundException("Department not found");

            if (!string.IsNullOrWhiteSpace(dto.Name)) row.Name = dto.Name.Trim();
            if (dto.Code != null) row.Code = dto.Code;
            if (dto.ParentId.HasValue) row.ParentId = dto.ParentId;
            if (dto.ManagerId.HasValue) row.ManagerId = dto.ManagerId;
            if (dto.Description != null) row.Description = dto.Description;
            if (dto.Position.HasValue) row.Position = dto.Position;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapDepartmentDto(row);
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            var row = await _db.Set<HrDepartment>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (row == null) throw new KeyNotFoundException("Department not found");
            row.IsDeleted = true;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private static HrEmployeeSalaryConfigDto MapSalaryConfigDto(HrEmployeeSalaryConfig x) => new()
        {
            Id = x.Id,
            UserId = x.UserId,
            GrossSalary = x.GrossSalary,
            IsHeadOfFamily = x.IsHeadOfFamily,
            ChildrenCount = x.ChildrenCount,
            CustomDeductions = x.CustomDeductions,
            BankAccount = x.BankAccount,
            CnssNumber = x.CnssNumber,
            HireDate = x.HireDate,
            Department = x.Department,
            Position = x.Position,
            EmploymentType = x.EmploymentType,
            Cin = x.Cin,
            BirthDate = x.BirthDate,
            MaritalStatus = x.MaritalStatus,
            AddressLine1 = x.AddressLine1,
            AddressLine2 = x.AddressLine2,
            City = x.City,
            PostalCode = x.PostalCode,
            EmergencyContactName = x.EmergencyContactName,
            EmergencyContactPhone = x.EmergencyContactPhone
        };

        private static HrAttendanceDto MapAttendanceDto(HrAttendanceRecord x) => new()
        {
            Id = x.Id,
            UserId = x.UserId,
            Date = x.Date,
            CheckIn = x.CheckIn?.ToString(@"hh\:mm"),
            CheckOut = x.CheckOut?.ToString(@"hh\:mm"),
            BreakDuration = x.BreakDuration,
            Source = x.Source,
            RawData = ParseJsonObject(x.RawData),
            HoursWorked = x.HoursWorked,
            OvertimeHours = x.OvertimeHours,
            Status = x.Status,
            Notes = x.Notes
        };

        private static HrAttendanceSettingsDto MapAttendanceSettings(HrAttendanceSettings x) => new()
        {
            Id = x.Id,
            WeekendDays = ParseJsonList<int>(x.WeekendDays),
            StandardHoursPerDay = x.StandardHoursPerDay,
            OvertimeThreshold = x.OvertimeThreshold,
            OvertimeMultiplier = x.OvertimeMultiplier,
            RoundingMethod = x.RoundingMethod,
            CalculationMethod = x.CalculationMethod,
            LateThresholdMinutes = x.LateThresholdMinutes,
            Holidays = ParseJsonList<string>(x.Holidays)
        };

        private static HrPayrollRunDto MapPayrollRunDto(HrPayrollRun run, List<HrPayrollEntry> entries, Dictionary<int, string> userMap)
        {
            return new HrPayrollRunDto
            {
                Id = run.Id,
                Month = run.Month,
                Year = run.Year,
                Status = run.Status,
                TotalGross = run.TotalGross,
                TotalNet = run.TotalNet,
                CreatedBy = run.CreatedBy,
                CreatedAt = run.CreatedAt,
                ConfirmedAt = run.ConfirmedAt,
                Entries = entries.Select(e => new HrPayrollEntryDto
                {
                    Id = e.Id,
                    PayrollRunId = e.PayrollRunId,
                    UserId = e.UserId,
                    UserName = userMap.ContainsKey(e.UserId) ? userMap[e.UserId] : string.Empty,
                    GrossSalary = e.GrossSalary,
                    Cnss = e.Cnss,
                    TaxableGross = e.TaxableGross,
                    Abattement = e.Abattement,
                    TaxableBase = e.TaxableBase,
                    Irpp = e.Irpp,
                    Css = e.Css,
                    NetSalary = e.NetSalary,
                    WorkedDays = e.WorkedDays,
                    TotalHours = e.TotalHours,
                    OvertimeHours = e.OvertimeHours,
                    LeaveDays = e.LeaveDays,
                    Details = ParseJsonObject(e.Details)
                }).ToList()
            };
        }

        private static HrDepartmentDto MapDepartmentDto(HrDepartment x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            Code = x.Code,
            ParentId = x.ParentId,
            ManagerId = x.ManagerId,
            Description = x.Description,
            Position = x.Position
        };

        private static TimeSpan? ParseTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return TimeSpan.TryParse(value, out var parsed) ? parsed : null;
        }

        private static List<T> ParseJsonList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        private static object? ParseJsonObject(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<object>(json);
            }
            catch
            {
                return null;
            }
        }

        private static object MapSafeUser(MyApi.Modules.Users.Models.User user)
        {
            return new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.IsActive,
                user.ProfilePictureUrl,
                user.CurrentStatus,
                user.CreatedDate,
                user.ModifiedDate
            };
        }
    }
}
