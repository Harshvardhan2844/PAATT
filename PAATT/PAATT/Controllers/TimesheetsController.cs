using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Controllers;

[Route("api/timesheets"), Authorize]
public sealed class TimesheetsController(ITimesheetService service) : ApiControllerBase
{
    [HttpGet("my"), Authorize(Roles = nameof(AppRole.Consultant))] public Task<IReadOnlyList<TimesheetDto>> Mine(CancellationToken token) => service.GetMineAsync(CurrentUserId, token);
    [HttpGet("review/{status}"), Authorize(Roles = nameof(AppRole.ProjectManager))] public Task<IReadOnlyList<TimesheetDto>> Review(TimesheetStatus status, CancellationToken token) => service.GetForManagerAsync(CurrentUserId, status, token);
    [HttpGet("{id:int}")] public Task<TimesheetDetailsDto> Details(int id, CancellationToken token) => service.GetDetailsAsync(id, CurrentUserId, User.IsInRole(nameof(AppRole.ProjectManager)), token);
    [HttpPost, Authorize(Roles = nameof(AppRole.Consultant))] public async Task<ActionResult<TimesheetDto>> Create(CreateTimesheetDto request, CancellationToken token) => Created("", await service.CreateAsync(CurrentUserId, request, token));
    [HttpPost("{id:int}/entries"), Authorize(Roles = nameof(AppRole.Consultant))] public Task<TimesheetDetailsDto> AddEntry(int id, CreateTimesheetEntryDto request, CancellationToken token) => service.AddEntryAsync(id, CurrentUserId, request, token);
    [HttpPost("{id:int}/submit"), Authorize(Roles = nameof(AppRole.Consultant))] public async Task<IActionResult> Submit(int id, CancellationToken token) { await service.SubmitAsync(id, CurrentUserId, token); return NoContent(); }
    [HttpPost("{id:int}/approve"), Authorize(Roles = nameof(AppRole.ProjectManager))] public async Task<IActionResult> Approve(int id, CancellationToken token) { await service.ApproveAsync(id, CurrentUserId, token); return NoContent(); }
    [HttpPost("{id:int}/reject"), Authorize(Roles = nameof(AppRole.ProjectManager))] public async Task<IActionResult> Reject(int id, RejectTimesheetDto request, CancellationToken token) { await service.RejectAsync(id, CurrentUserId, request, token); return NoContent(); }
}
