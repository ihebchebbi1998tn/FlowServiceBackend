using System;
using System.Linq;
using MyApi.Modules.Dispatches.DTOs;
using MyApi.Modules.Dispatches.Models;

namespace MyApi.Modules.Dispatches.Mapping
{
    // Lightweight mapping helpers (avoid adding AutoMapper dependency in scaffold)
    public static class DispatchMapping
    {
        // technicianNames: optional map of technicianId -> display name to populate UserLightDto.Name
        public static DispatchDto ToDto(Dispatch src, System.Collections.Generic.Dictionary<int, string>? technicianNames = null)
        {
            return new DispatchDto
            {
                Id = src.Id,
                DispatchNumber = src.DispatchNumber,
                JobId = int.TryParse(src.JobId, out var jid) ? jid : null,
                ServiceOrderId = src.ServiceOrderId,
                Status = src.Status,
                Priority = src.Priority,
                CompletionPercentage = src.CompletionPercentage,
                DispatchedAt = src.DispatchedAt,
                DispatchedBy = src.DispatchedBy,
                CreatedAt = src.CreatedDate,
                UpdatedAt = src.ModifiedDate ?? src.CreatedDate,
                AssignedTechnicians = src.AssignedTechnicians?.Select(at => new UserLightDto 
                { 
                    Id = at.TechnicianId,
                    Name = technicianNames != null && technicianNames.TryGetValue(at.TechnicianId, out var n) ? n : null
                }).ToList() ?? new(),
                Scheduling = new SchedulingDto
                {
                    ScheduledDate = src.ScheduledDate,
                    EstimatedDuration = src.ActualDuration
                },
                TimeEntries = src.TimeEntries?.Cast<object>().ToList() ?? new(),
                Expenses = src.Expenses?.Cast<object>().ToList() ?? new(),
                MaterialsUsed = src.MaterialsUsed?.Cast<object>().ToList() ?? new(),
                Attachments = src.Attachments?.Cast<object>().ToList() ?? new(),
                Notes = src.Notes?.Cast<object>().ToList() ?? new()
            };
        }
    }
}
