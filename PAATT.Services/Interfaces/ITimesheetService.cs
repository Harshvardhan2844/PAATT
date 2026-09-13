using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Services.Interfaces;

public interface ITimesheetService
{
    Task<IReadOnlyList<TimesheetDto>> GetMineAsync(string consultantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetDto>> GetForManagerAsync(string managerId, TimesheetStatus status, CancellationToken cancellationToken = default);
    Task<TimesheetDetailsDto> GetDetailsAsync(int id, string userId, bool canManage, CancellationToken cancellationToken = default);
    Task<TimesheetDto> CreateAsync(string consultantId, CreateTimesheetDto request, CancellationToken cancellationToken = default);
    Task<TimesheetDetailsDto> AddEntryAsync(int timesheetId, string consultantId, CreateTimesheetEntryDto request, CancellationToken cancellationToken = default);
    Task<TimesheetDetailsDto> UpdateEntryAsync(int entryId, string consultantId, UpdateTimesheetEntryDto request, CancellationToken cancellationToken = default);
    Task DeleteEntryAsync(int entryId, string consultantId, CancellationToken cancellationToken = default);
    Task SubmitAsync(int timesheetId, string consultantId, CancellationToken cancellationToken = default);
    Task ApproveAsync(int timesheetId, string managerId, CancellationToken cancellationToken = default);
    Task RejectAsync(int timesheetId, string managerId, RejectTimesheetDto request, CancellationToken cancellationToken = default);
    Task RejectEntryAsync(int entryId, string managerId, RejectTimesheetEntryDto request, CancellationToken cancellationToken = default);
}
