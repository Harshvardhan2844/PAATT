using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class TimesheetEntryService : ITimesheetEntryService
{
    private readonly ApplicationDbContext _db;

    public TimesheetEntryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<TimesheetEntryDto>> AddEntryAsync(
        int timesheetId, DateOnly date, decimal hoursWorked, string? description)
    {
        if (hoursWorked <= 0)
            return ServiceResult<TimesheetEntryDto>.Fail("Hours worked must be greater than zero.");

        if (hoursWorked > TimeRules.MaxHoursPerDay)
            return ServiceResult<TimesheetEntryDto>.Fail($"Hours worked cannot exceed {TimeRules.MaxHoursPerDay} in a single entry.");

        if (description?.Length > 1000)
            return ServiceResult<TimesheetEntryDto>.Fail("Description cannot exceed 1000 characters.");

        var timesheet = await _db.Timesheets.FirstOrDefaultAsync(t => t.Id == timesheetId);
        if (timesheet is null)
            return ServiceResult<TimesheetEntryDto>.Fail("Timesheet not found.");

        // Submitting closes the week. A Rejected timesheet is open again,
        // because the whole point of rejection is "fix it and resend".
        if (!IsOpen(timesheet.Status))
            return ServiceResult<TimesheetEntryDto>.Fail(
                $"This timesheet has been {timesheet.Status.ToString().ToLowerInvariant()} — no more time can be added to this week.");

        var today = TimeRules.Today;
        if (date != today)
            return ServiceResult<TimesheetEntryDto>.Fail("Entries can only be logged for the current calendar day.");

        var capCheck = await CheckDailyCapAsync(timesheet.EmployeeId, date, hoursWorked, excludeEntryId: null);
        if (!capCheck.Success)
            return ServiceResult<TimesheetEntryDto>.Fail(capCheck.Errors);

        var entry = new TimesheetEntry
        {
            TimesheetId = timesheetId,
            Date = date,
            HoursWorked = hoursWorked,
            Description = NormalizeDescription(description),
            Status = EntryStatus.Pending
        };
        _db.TimesheetEntries.Add(entry);
        await _db.SaveChangesAsync();

        return ServiceResult<TimesheetEntryDto>.Ok(ToDto(entry));
    }

    public async Task<ServiceResult<TimesheetEntryDto>> UpdateEntryAsync(
        int entryId, decimal hoursWorked, string? description)
    {
        if (hoursWorked <= 0)
            return ServiceResult<TimesheetEntryDto>.Fail("Hours worked must be greater than zero.");

        if (hoursWorked > TimeRules.MaxHoursPerDay)
            return ServiceResult<TimesheetEntryDto>.Fail($"Hours worked cannot exceed {TimeRules.MaxHoursPerDay} in a single entry.");

        if (description?.Length > 1000)
            return ServiceResult<TimesheetEntryDto>.Fail("Description cannot exceed 1000 characters.");

        var entry = await _db.TimesheetEntries
            .Include(e => e.Timesheet)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry is null)
            return ServiceResult<TimesheetEntryDto>.Fail("Entry not found.");

        if (!IsEditable(entry))
            return ServiceResult<TimesheetEntryDto>.Fail(LockReason(entry));

        var capCheck = await CheckDailyCapAsync(entry.Timesheet.EmployeeId, entry.Date, hoursWorked, excludeEntryId: entry.Id);
        if (!capCheck.Success)
            return ServiceResult<TimesheetEntryDto>.Fail(capCheck.Errors);

        entry.HoursWorked = hoursWorked;
        entry.Description = NormalizeDescription(description);

        // Editing a manager-rejected entry sends it back for re-review.
        if (entry.Status == EntryStatus.Rejected)
        {
            entry.Status = EntryStatus.Pending;
            entry.ManagerFeedback = null;
        }

        await _db.SaveChangesAsync();
        return ServiceResult<TimesheetEntryDto>.Ok(ToDto(entry));
    }

    public async Task<ServiceResult> DeleteEntryAsync(int entryId)
    {
        var entry = await _db.TimesheetEntries
            .Include(e => e.Timesheet)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry is null)
            return ServiceResult.Fail("Entry not found.");

        if (!IsEditable(entry))
            return ServiceResult.Fail(LockReason(entry));

        _db.TimesheetEntries.Remove(entry);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectEntryAsync(int entryId, string managerId, string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
            return ServiceResult.Fail("Feedback is required when rejecting an entry.");

        if (feedback.Length > 1000)
            return ServiceResult.Fail("Feedback cannot exceed 1000 characters.");

        var entry = await _db.TimesheetEntries
            .Include(e => e.Timesheet)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry is null)
            return ServiceResult.Fail("Entry not found.");

        // Manager is a per-project role, so this is the check that matters —
        // before Phase 5 this method took anyone's word for it.
        var managesProject = await _db.ProjectAssignments.AnyAsync(a =>
            a.ProjectId == entry.Timesheet.ProjectId
            && a.EmployeeId == managerId
            && a.AssignmentType == AssignmentType.Manager);

        if (!managesProject)
            return ServiceResult.Fail("You don't manage this project.");

        // A final approval closes the whole timesheet. Entry-level decisions
        // are only possible while the week is awaiting the manager's review.
        if (entry.Timesheet.Status != TimesheetStatus.Submitted)
            return ServiceResult.Fail($"Entries can't be rejected on a {entry.Timesheet.Status.ToString().ToLowerInvariant()} timesheet.");

        if (entry.Status == EntryStatus.Rejected)
            return ServiceResult.Fail("That entry has already been rejected and is waiting on the consultant.");

        entry.Status = EntryStatus.Rejected;
        entry.ManagerFeedback = feedback.Trim();

        // The feedback above is overwritten the moment the consultant edits
        // the entry, so the history row is the durable record.
        var actorName = await _db.Users
            .Where(u => u.Id == managerId)
            .Select(u => u.Name)
            .FirstOrDefaultAsync();

        _db.TimesheetReviewEvents.Add(new TimesheetReviewEvent
        {
            TimesheetId = entry.TimesheetId,
            TimesheetEntryId = entry.Id,
            EntryDate = entry.Date,
            Action = ReviewAction.EntryRejected,
            ActorId = managerId,
            ActorName = actorName ?? string.Empty,
            Feedback = feedback.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<string?> GetEntryOwnerIdAsync(int entryId)
    {
        return await _db.TimesheetEntries
            .Where(e => e.Id == entryId)
            .Select(e => e.Timesheet.EmployeeId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// An entry is open for editing when a manager has rejected it — that
    /// reopens it no matter how old it is or what state the timesheet is in —
    /// or when it's today's entry on a week that hasn't been submitted yet.
    /// </summary>
    private static bool IsEditable(TimesheetEntry entry)
    {
        if (entry.Status == EntryStatus.Rejected) return true;
        return entry.Date == TimeRules.Today && IsOpen(entry.Timesheet.Status);
    }

    private static bool IsOpen(TimesheetStatus status) =>
        status is TimesheetStatus.Draft or TimesheetStatus.Rejected;

    private static string LockReason(TimesheetEntry entry) =>
        IsOpen(entry.Timesheet.Status)
            ? "That day has closed. Entries can only be changed on the day they were logged, or after a manager rejects them."
            : $"This timesheet has been {entry.Timesheet.Status.ToString().ToLowerInvariant()} — its entries can't be changed unless a manager rejects them.";

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    /// <summary>
    /// Cross-project 24-hr/day validation: sums HoursWorked for this employee,
    /// for this date, across EVERY one of their timesheets (any project) —
    /// not just the one the entry being added/edited belongs to — then adds
    /// the candidate hours and checks against the cap. When editing an
    /// existing entry, its own current hours are excluded from the running
    /// total first so the entry isn't double-counted against itself.
    /// </summary>
    private async Task<ServiceResult> CheckDailyCapAsync(
        string employeeId, DateOnly date, decimal candidateHours, int? excludeEntryId)
    {
        var query = _db.TimesheetEntries
            .Where(e => e.Timesheet.EmployeeId == employeeId && e.Date == date);

        if (excludeEntryId is not null)
            query = query.Where(e => e.Id != excludeEntryId.Value);

        var existingTotal = await query.SumAsync(e => (decimal?)e.HoursWorked) ?? 0m;

        if (existingTotal + candidateHours > TimeRules.MaxHoursPerDay)
        {
            var remaining = TimeRules.MaxHoursPerDay - existingTotal;
            return ServiceResult.Fail(
                $"Total hours logged for {date:yyyy-MM-dd} across all projects cannot exceed {TimeRules.MaxHoursPerDay}. " +
                $"You have {existingTotal} hour(s) logged already; at most {Math.Max(remaining, 0)} more can be added.");
        }

        return ServiceResult.Ok();
    }

    private static TimesheetEntryDto ToDto(TimesheetEntry e)
    {
        var today = TimeRules.Today;
        return new TimesheetEntryDto
        {
            Id = e.Id,
            TimesheetId = e.TimesheetId,
            Date = e.Date,
            HoursWorked = e.HoursWorked,
            Description = e.Description,
            Status = e.Status,
            ManagerFeedback = e.ManagerFeedback,
            IsLocked = e.Date < today && e.Status != EntryStatus.Rejected
        };
    }
}
