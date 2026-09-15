using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class TimesheetService : ITimesheetService
{
    private readonly ApplicationDbContext _db;

    public TimesheetService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<TimesheetDto>> GetOrCreateTimesheetAsync(
        string employeeId, int projectId, DateOnly weekStartingDate)
    {
        var isAssignedConsultant = await _db.ProjectAssignments.AnyAsync(a =>
            a.ProjectId == projectId && a.EmployeeId == employeeId && a.AssignmentType == AssignmentType.Consultant);

        if (!isAssignedConsultant)
            return ServiceResult<TimesheetDto>.Fail("You are not assigned as a Consultant on this project.");

        var existing = await _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId
                                   && t.ProjectId == projectId
                                   && t.WeekStartingDate == weekStartingDate);

        if (existing is not null)
            return ServiceResult<TimesheetDto>.Ok(ToDto(existing));

        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            ProjectId = projectId,
            WeekStartingDate = weekStartingDate,
            Status = TimesheetStatus.Draft
        };
        _db.Timesheets.Add(timesheet);
        await _db.SaveChangesAsync();

        // Reload with navigation properties populated for the DTO.
        var created = await _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .FirstAsync(t => t.Id == timesheet.Id);

        return ServiceResult<TimesheetDto>.Ok(ToDto(created));
    }

    public async Task<TimesheetDto?> GetByIdAsync(int timesheetId)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == timesheetId);

        return timesheet is null ? null : ToDto(timesheet);
    }

    public async Task<TimesheetDto?> GetTimesheetForManagerAsync(int timesheetId, string managerId)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == timesheetId);

        if (timesheet is null) return null;

        // A manager who doesn't manage this project gets the same answer as
        // if it didn't exist, rather than a hint that it does.
        if (!await ManagesProjectAsync(managerId, timesheet.ProjectId)) return null;

        // Nor do they see a week that hasn't been sent to them yet.
        if (timesheet.Status == TimesheetStatus.Draft) return null;

        return ToDto(timesheet);
    }

    public async Task<List<TimesheetDto>> GetTimesheetsForEmployeeAsync(string employeeId, int? projectId = null)
    {
        var query = _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .Where(t => t.EmployeeId == employeeId);

        if (projectId is not null)
            query = query.Where(t => t.ProjectId == projectId);

        var timesheets = await query
            .OrderByDescending(t => t.WeekStartingDate)
            .ToListAsync();

        return timesheets.Select(ToDto).ToList();
    }

    public async Task<List<TimesheetDto>> GetTimesheetsForManagerAsync(string managerId, TimesheetStatus? status = null)
    {
        var query = _db.Timesheets
            .Include(t => t.Employee)
            .Include(t => t.Project)
            .Include(t => t.Entries)
            .Where(t => t.Project.Assignments.Any(a =>
                a.EmployeeId == managerId && a.AssignmentType == AssignmentType.Manager))
            // An unsubmitted week is the consultant's own workspace — it isn't
            // in the manager's queue until they send it.
            .Where(t => t.Status != TimesheetStatus.Draft);

        if (status is not null)
            query = query.Where(t => t.Status == status);

        var timesheets = await query
            .OrderByDescending(t => t.WeekStartingDate)
            .ToListAsync();

        return timesheets.Select(ToDto).ToList();
    }

    public async Task<List<TimesheetReviewEventDto>> GetReviewHistoryAsync(int timesheetId)
    {
        return await _db.TimesheetReviewEvents
            .Where(re => re.TimesheetId == timesheetId)
            .OrderBy(re => re.OccurredAtUtc).ThenBy(re => re.Id)
            .Select(re => new TimesheetReviewEventDto
            {
                Id = re.Id,
                TimesheetId = re.TimesheetId,
                TimesheetEntryId = re.TimesheetEntryId,
                EntryDate = re.EntryDate,
                Action = re.Action,
                ActorName = re.ActorName,
                Feedback = re.Feedback,
                OccurredAtUtc = re.OccurredAtUtc
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> SubmitTimesheetAsync(int timesheetId)
    {
        var timesheet = await _db.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.Id == timesheetId);

        if (timesheet is null)
            return ServiceResult.Fail("Timesheet not found.");

        if (timesheet.Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
            return ServiceResult.Fail($"Cannot submit a timesheet in {timesheet.Status} status.");

        if (timesheet.Entries.Count == 0)
            return ServiceResult.Fail("Cannot submit a timesheet with no entries.");

        timesheet.Status = TimesheetStatus.Submitted;

        // Logged on every submission, so a week that bounced twice reads as
        // submit -> reject -> submit -> reject in the history.
        _db.TimesheetReviewEvents.Add(new TimesheetReviewEvent
        {
            TimesheetId = timesheet.Id,
            Action = ReviewAction.Submitted,
            ActorId = timesheet.EmployeeId,
            ActorName = timesheet.Employee?.Name ?? string.Empty,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApproveTimesheetAsync(int timesheetId, string managerId)
    {
        var timesheet = await _db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == timesheetId);
        if (timesheet is null)
            return ServiceResult.Fail("Timesheet not found.");

        if (!await ManagesProjectAsync(managerId, timesheet.ProjectId))
            return ServiceResult.Fail("You don't manage this project.");

        if (timesheet.Status != TimesheetStatus.Submitted)
            return ServiceResult.Fail("Only a Submitted timesheet can be approved.");

        timesheet.Status = TimesheetStatus.Approved;
        timesheet.OverallManagerFeedback = null;

        // Entries the manager already rejected individually stay rejected —
        // approving the week doesn't quietly wave those through.
        foreach (var entry in timesheet.Entries.Where(e => e.Status != EntryStatus.Rejected))
        {
            entry.Status = EntryStatus.Approved;
        }

        _db.TimesheetReviewEvents.Add(await BuildEventAsync(
            timesheet.Id, ReviewAction.Approved, managerId, feedback: null));

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectTimesheetAsync(int timesheetId, string managerId, string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            return ServiceResult.Fail("Feedback is required when rejecting a timesheet.");

        if (feedback.Length > 1000)
            return ServiceResult.Fail("Feedback cannot exceed 1000 characters.");

        var timesheet = await _db.Timesheets.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == timesheetId);
        if (timesheet is null)
            return ServiceResult.Fail("Timesheet not found.");

        if (!await ManagesProjectAsync(managerId, timesheet.ProjectId))
            return ServiceResult.Fail("You don't manage this project.");

        if (timesheet.Status != TimesheetStatus.Submitted)
            return ServiceResult.Fail("Only a Submitted timesheet can be rejected.");

        timesheet.Status = TimesheetStatus.Rejected;
        timesheet.OverallManagerFeedback = feedback.Trim();

        // Reopen every entry (regardless of date) so the consultant can fix and resubmit.
        foreach (var entry in timesheet.Entries)
        {
            entry.Status = EntryStatus.Rejected;
        }

        _db.TimesheetReviewEvents.Add(await BuildEventAsync(
            timesheet.Id, ReviewAction.Rejected, managerId, feedback.Trim()));

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    /// <summary>
    /// True when this person holds the Manager assignment on the project.
    /// Manager is a per-project role, not an Identity role, so this is the
    /// only meaningful authorization check for manager actions.
    /// </summary>
    private Task<bool> ManagesProjectAsync(string managerId, int projectId) =>
        _db.ProjectAssignments.AnyAsync(a =>
            a.ProjectId == projectId && a.EmployeeId == managerId && a.AssignmentType == AssignmentType.Manager);

    private async Task<TimesheetReviewEvent> BuildEventAsync(
        int timesheetId, ReviewAction action, string actorId, string? feedback)
    {
        // Name is snapshotted rather than joined, so history still reads
        // correctly if the person is later renamed or removed.
        var actorName = await _db.Users
            .Where(u => u.Id == actorId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();

        return new TimesheetReviewEvent
        {
            TimesheetId = timesheetId,
            Action = action,
            ActorId = actorId,
            ActorName = actorName ?? string.Empty,
            Feedback = feedback,
            OccurredAtUtc = DateTime.UtcNow
        };
    }

    private static TimesheetDto ToDto(Timesheet t)
    {
        var today = TimeRules.Today;
        var entries = t.Entries
            .OrderBy(e => e.Date)
            .Select(e => new TimesheetEntryDto
            {
                Id = e.Id,
                TimesheetId = e.TimesheetId,
                Date = e.Date,
                HoursWorked = e.HoursWorked,
                Description = e.Description,
                Status = e.Status,
                ManagerFeedback = e.ManagerFeedback,
                IsLocked = e.Date < today && e.Status != EntryStatus.Rejected
            })
            .ToList();

        return new TimesheetDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            EmployeeName = t.Employee?.Name ?? string.Empty,
            ProjectId = t.ProjectId,
            ProjectName = t.Project?.Name ?? string.Empty,
            WeekStartingDate = t.WeekStartingDate,
            Status = t.Status,
            OverallManagerFeedback = t.OverallManagerFeedback,
            Entries = entries,
            TotalHours = entries.Sum(e => e.HoursWorked)
        };
    }
}
