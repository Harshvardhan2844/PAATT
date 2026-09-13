using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/timesheet-entries"), Authorize]
public sealed class TimesheetEntriesController(ITimesheetService service) : ApiControllerBase
{
    [HttpPut("{id:int}"), Authorize(Roles = nameof(AppRole.Consultant))] public Task<TimesheetDetailsDto> Update(int id, UpdateTimesheetEntryDto request, CancellationToken token) => service.UpdateEntryAsync(id, CurrentUserId, request, token);
    [HttpDelete("{id:int}"), Authorize(Roles = nameof(AppRole.Consultant))] public async Task<IActionResult> Delete(int id, CancellationToken token) { await service.DeleteEntryAsync(id, CurrentUserId, token); return NoContent(); }
    [HttpPost("{id:int}/reject"), Authorize(Roles = nameof(AppRole.ProjectManager))] public async Task<IActionResult> Reject(int id, RejectTimesheetEntryDto request, CancellationToken token) { await service.RejectEntryAsync(id, CurrentUserId, request, token); return NoContent(); }
}
