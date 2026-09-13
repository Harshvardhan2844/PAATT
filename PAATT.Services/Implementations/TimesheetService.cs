using Microsoft.EntityFrameworkCore;
using PAATT.Data;
using PAATT.Data.Entities;
using PAATT.Shared;
using PAATT.Services.Interfaces;
using PAATT.Shared.DTOs;
using PAATT.Shared.Enums;

namespace PAATT.Services.Implementations;

public sealed class TimesheetService(ApplicationDbContext database) : ITimesheetService
{
    public async Task<IReadOnlyList<TimesheetDto>> GetMineAsync(string consultantId, CancellationToken cancellationToken = default) =>
        await SummaryQuery().Where(sheet => sheet.ConsultantId == consultantId).OrderByDescending(sheet => sheet.WeekStartDate).Select(Summary()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TimesheetDto>> GetForManagerAsync(string managerId, TimesheetStatus status, CancellationToken cancellationToken = default) =>
        await SummaryQuery().Where(sheet => sheet.Project.ProjectManagerId == managerId && sheet.Status == status).OrderByDescending(sheet => sheet.WeekStartDate).Select(Summary()).ToListAsync(cancellationToken);

    public async Task<TimesheetDetailsDto> GetDetailsAsync(int id, string userId, bool canManage, CancellationToken cancellationToken = default)
    {
        var sheet = await DetailsQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new ApplicationValidationException("Timesheet not found.");
        if (sheet.ConsultantId != userId && (!canManage || sheet.Project.ProjectManagerId != userId))
            throw new ApplicationValidationException("You do not have access to this timesheet.");
        return ToDetails(sheet);
    }

    public async Task<TimesheetDto> CreateAsync(string consultantId, CreateTimesheetDto request, CancellationToken cancellationToken = default)
    {
        var weekStart = GetWeekStart(request.WeekStartDate);
        if (!await database.ProjectAssignments.AnyAsync(assignment => assignment.ProjectId == request.ProjectId && assignment.ConsultantId == consultantId, cancellationToken))
            throw new ApplicationValidationException("Consultant is not assigned to this project.");
        if (await database.Timesheets.AnyAsync(sheet => sheet.ConsultantId == consultantId && sheet.ProjectId == request.ProjectId && sheet.WeekStartDate == weekStart, cancellationToken))
            throw new ApplicationValidationException("A timesheet already exists for this project and week.");
        var sheet = new Timesheet { ConsultantId = consultantId, ProjectId = request.ProjectId, WeekStartDate = weekStart };
        database.Timesheets.Add(sheet);
        await database.SaveChangesAsync(cancellationToken);
        return await SummaryQuery().Where(item => item.Id == sheet.Id).Select(Summary()).SingleAsync(cancellationToken);
    }

    public async Task<TimesheetDetailsDto> AddEntryAsync(int timesheetId, string consultantId, CreateTimesheetEntryDto request, CancellationToken cancellationToken = default)
    {
        var sheet = await FindOwnedSheetAsync(timesheetId, consultantId, cancellationToken);
        EnsureEntryCanBeChanged(sheet, null);
        ValidateEntryDate(sheet, request.WorkDate);
        await EnsureDailyHoursAsync(consultantId, request.WorkDate, request.Hours, null, cancellationToken);
        sheet.Entries.Add(new TimesheetEntry { WorkDate = request.WorkDate, Hours = request.Hours, Description = request.Description.Trim() });
        await database.SaveChangesAsync(cancellationToken);
        return ToDetails(await GetTrackedDetailsAsync(timesheetId, cancellationToken));
    }

    public async Task<TimesheetDetailsDto> UpdateEntryAsync(int entryId, string consultantId, UpdateTimesheetEntryDto request, CancellationToken cancellationToken = default)
    {
        var entry = await database.TimesheetEntries.Include(item => item.Timesheet).SingleOrDefaultAsync(item => item.Id == entryId, cancellationToken) ?? throw new ApplicationValidationException("Entry not found.");
        if (entry.Timesheet.ConsultantId != consultantId)
            throw new ApplicationValidationException("You do not have access to this entry.");
        EnsureEntryCanBeChanged(entry.Timesheet, entry);
        ValidateEntryDate(entry.Timesheet, request.WorkDate);
        await EnsureDailyHoursAsync(consultantId, request.WorkDate, request.Hours, entry.Id, cancellationToken);
        entry.WorkDate = request.WorkDate;
        entry.Hours = request.Hours;
        entry.Description = request.Description.Trim();
        entry.Status = TimesheetEntryStatus.Pending;
        entry.Feedback = null;
        await database.SaveChangesAsync(cancellationToken);
        return ToDetails(await GetTrackedDetailsAsync(entry.TimesheetId, cancellationToken));
    }

    public async Task DeleteEntryAsync(int entryId, string consultantId, CancellationToken cancellationToken = default)
    {
        var entry = await database.TimesheetEntries.Include(item => item.Timesheet).SingleOrDefaultAsync(item => item.Id == entryId, cancellationToken) ?? throw new ApplicationValidationException("Entry not found.");
        if (entry.Timesheet.ConsultantId != consultantId)
            throw new ApplicationValidationException("You do not have access to this entry.");
        EnsureEntryCanBeChanged(entry.Timesheet, entry);
        database.TimesheetEntries.Remove(entry);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(int timesheetId, string consultantId, CancellationToken cancellationToken = default)
    {
        var sheet = await FindOwnedSheetAsync(timesheetId, consultantId, cancellationToken);
        if (sheet.Status is not (TimesheetStatus.Draft or TimesheetStatus.Rejected))
            throw new ApplicationValidationException("Only draft or rejected timesheets can be submitted.");
        if (sheet.Entries.Count == 0)
            throw new ApplicationValidationException("Add at least one entry before submitting a timesheet.");
        sheet.Status = TimesheetStatus.Pending;
        sheet.SubmittedUtc = DateTime.UtcNow;
        foreach (var entry in sheet.Entries)
        {
            entry.Status = TimesheetEntryStatus.Pending;
            entry.Feedback = null;
        }
        AddReview(sheet, consultantId, TimesheetReviewAction.Submitted, null);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(int timesheetId, string managerId, CancellationToken cancellationToken = default)
    {
        var sheet = await FindManagedSheetAsync(timesheetId, managerId, cancellationToken);
        EnsurePending(sheet);
        sheet.Status = TimesheetStatus.Approved;
        foreach (var entry in sheet.Entries.Where(entry => entry.Status == TimesheetEntryStatus.Pending)) entry.Status = TimesheetEntryStatus.Approved;
        AddReview(sheet, managerId, TimesheetReviewAction.Approved, null);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(int timesheetId, string managerId, RejectTimesheetDto request, CancellationToken cancellationToken = default)
    {
        var sheet = await FindManagedSheetAsync(timesheetId, managerId, cancellationToken);
        EnsurePending(sheet);
        var feedback = RequireFeedback(request.Feedback);
        sheet.Status = TimesheetStatus.Rejected;
        AddReview(sheet, managerId, TimesheetReviewAction.Rejected, feedback);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectEntryAsync(int entryId, string managerId, RejectTimesheetEntryDto request, CancellationToken cancellationToken = default)
    {
        var entry = await database.TimesheetEntries.Include(item => item.Timesheet).ThenInclude(sheet => sheet.Project).SingleOrDefaultAsync(item => item.Id == entryId, cancellationToken) ?? throw new ApplicationValidationException("Entry not found.");
        if (entry.Timesheet.Project.ProjectManagerId != managerId) throw new ApplicationValidationException("You can only review timesheets for your projects.");
        EnsurePending(entry.Timesheet);
        var feedback = RequireFeedback(request.Feedback);
        entry.Status = TimesheetEntryStatus.Rejected;
        entry.Feedback = feedback;
        AddReview(entry.Timesheet, managerId, TimesheetReviewAction.Rejected, feedback, entry.Id);
        await database.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Timesheet> SummaryQuery() => database.Timesheets.AsNoTracking();
    private IQueryable<Timesheet> DetailsQuery() => database.Timesheets.AsNoTracking().Include(sheet => sheet.Consultant).Include(sheet => sheet.Project).ThenInclude(project => project.ProjectManager).Include(sheet => sheet.Entries).Include(sheet => sheet.Reviews).ThenInclude(review => review.Reviewer);
    private async Task<Timesheet> GetTrackedDetailsAsync(int id, CancellationToken token) => await database.Timesheets.Include(sheet => sheet.Consultant).Include(sheet => sheet.Project).Include(sheet => sheet.Entries).Include(sheet => sheet.Reviews).ThenInclude(review => review.Reviewer).SingleAsync(sheet => sheet.Id == id, token);
    private async Task<Timesheet> FindOwnedSheetAsync(int id, string consultantId, CancellationToken token) => await database.Timesheets.Include(sheet => sheet.Entries).SingleOrDefaultAsync(sheet => sheet.Id == id && sheet.ConsultantId == consultantId, token) ?? throw new ApplicationValidationException("Timesheet not found.");
    private async Task<Timesheet> FindManagedSheetAsync(int id, string managerId, CancellationToken token) => await database.Timesheets.Include(sheet => sheet.Project).Include(sheet => sheet.Entries).SingleOrDefaultAsync(sheet => sheet.Id == id && sheet.Project.ProjectManagerId == managerId, token) ?? throw new ApplicationValidationException("Timesheet not found or not managed by you.");
    private static System.Linq.Expressions.Expression<Func<Timesheet, TimesheetDto>> Summary() => sheet => new TimesheetDto(sheet.Id, sheet.ProjectId, sheet.Project.Name, sheet.ConsultantId, sheet.Consultant.Name, sheet.WeekStartDate, sheet.Status, sheet.Entries.Sum(entry => entry.Hours));
    private static TimesheetDetailsDto ToDetails(Timesheet sheet) => new(sheet.Id, sheet.ProjectId, sheet.Project.Name, sheet.ConsultantId, sheet.Consultant.Name, sheet.WeekStartDate, sheet.Status, sheet.Entries.Sum(entry => entry.Hours), sheet.Entries.OrderBy(entry => entry.WorkDate).Select(entry => new TimesheetEntryDto(entry.Id, entry.WorkDate, entry.Hours, entry.Description, entry.Status, entry.Feedback)).ToList(), sheet.Reviews.OrderByDescending(review => review.CreatedUtc).Select(review => new TimesheetReviewDto(review.Id, review.TimesheetEntryId, review.Reviewer.Name, review.Action, review.Feedback, review.CreatedUtc)).ToList());
    private static DateOnly GetWeekStart(DateOnly date) => date.AddDays(-((7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7));
    private static void ValidateEntryDate(Timesheet sheet, DateOnly workDate) { if (workDate != AppClock.Today || GetWeekStart(workDate) != sheet.WeekStartDate) throw new ApplicationValidationException("Timesheet entries can only be created or changed for the current date."); }
    private async Task EnsureDailyHoursAsync(string consultantId, DateOnly date, decimal requestedHours, int? excludedEntryId, CancellationToken token) { var total = await database.TimesheetEntries.Where(entry => entry.Timesheet.ConsultantId == consultantId && entry.WorkDate == date && (!excludedEntryId.HasValue || entry.Id != excludedEntryId.Value)).SumAsync(entry => (decimal?)entry.Hours, token) ?? 0; if (total + requestedHours > 24) throw new ApplicationValidationException("Daily logged hours cannot exceed 24 hours."); }
    private static void EnsureEntryCanBeChanged(Timesheet sheet, TimesheetEntry? entry) { if (sheet.Status == TimesheetStatus.Draft) return; if (sheet.Status == TimesheetStatus.Rejected) return; if (entry?.Status == TimesheetEntryStatus.Rejected) return; throw new ApplicationValidationException("Only draft or rejected timesheet entries can be changed."); }
    private static void EnsurePending(Timesheet sheet) { if (sheet.Status != TimesheetStatus.Pending) throw new ApplicationValidationException("Only pending timesheets can be reviewed."); }
    private static string RequireFeedback(string feedback) => string.IsNullOrWhiteSpace(feedback) ? throw new ApplicationValidationException("Rejection feedback is required.") : feedback.Trim();
    private static void AddReview(Timesheet sheet, string reviewerId, TimesheetReviewAction action, string? feedback, int? entryId = null) => sheet.Reviews.Add(new TimesheetReview { ReviewerId = reviewerId, Action = action, Feedback = feedback, TimesheetEntryId = entryId });
}
