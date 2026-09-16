using App.Data;
using App.Data.Entities;
using App.Service.Interfaces;
using App.Shared;
using App.Shared.Dtos;
using App.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Service.Implementations;

public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectAssignmentService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<ProjectAssignmentDto>> GetAssignmentsForProjectAsync(int projectId)
    {
        var rows = await _db.ProjectAssignments
            .Include(a => a.Project)
            .Include(a => a.Employee)
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.AssignmentType).ThenBy(a => a.Employee.Name)
            .ToListAsync();

        return rows.Select(ToDto).ToList();
    }

    public async Task<List<ProjectAssignmentDto>> GetAssignmentsForEmployeeAsync(string employeeId)
    {
        var rows = await _db.ProjectAssignments
            .Include(a => a.Project)
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.Project.Name)
            .ToListAsync();

        return rows.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ProjectAssignmentDto>> AssignAsync(
        int projectId, string employeeId, AssignmentType assignmentType, decimal? budgetedHours)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project is null)
            return ServiceResult<ProjectAssignmentDto>.Fail("Project not found.");

        var employee = await _db.Users.FindAsync(employeeId);
        if (employee is null)
            return ServiceResult<ProjectAssignmentDto>.Fail("Employee not found.");

        if (assignmentType == AssignmentType.Consultant && (budgetedHours is null || budgetedHours <= 0))
            return ServiceResult<ProjectAssignmentDto>.Fail("Budgeted hours are required for a Consultant assignment.");

        var existingForEmployee = await _db.ProjectAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.EmployeeId == employeeId);

        // An employee can't be both Manager and Consultant on the same project.
        // If a row already exists for this (project, employee) pair, this call
        // updates it in place rather than creating a conflicting second row —
        // that also covers "change someone from Consultant to Manager" cleanly.
        if (assignmentType == AssignmentType.Manager)
        {
            var existingManager = await _db.ProjectAssignments
                .FirstOrDefaultAsync(a => a.ProjectId == projectId
                                        && a.AssignmentType == AssignmentType.Manager
                                        && a.EmployeeId != employeeId);
            if (existingManager is not null)
                return ServiceResult<ProjectAssignmentDto>.Fail(
                    "This project already has a different Manager assigned. Remove that assignment first.");
        }

        ProjectAssignment assignment;
        if (existingForEmployee is not null)
        {
            existingForEmployee.AssignmentType = assignmentType;
            existingForEmployee.BudgetedHours = assignmentType == AssignmentType.Consultant ? budgetedHours : null;
            assignment = existingForEmployee;
        }
        else
        {
            assignment = new ProjectAssignment
            {
                ProjectId = projectId,
                EmployeeId = employeeId,
                AssignmentType = assignmentType,
                BudgetedHours = assignmentType == AssignmentType.Consultant ? budgetedHours : null
            };
            _db.ProjectAssignments.Add(assignment);
        }

        await _db.SaveChangesAsync();
        await SynchronizeManagerRoleAsync(employeeId);

        return ServiceResult<ProjectAssignmentDto>.Ok(new ProjectAssignmentDto
        {
            Id = assignment.Id,
            ProjectId = projectId,
            ProjectName = project.Name,
            EmployeeId = employeeId,
            EmployeeName = employee.Name,
            AssignmentType = assignment.AssignmentType,
            BudgetedHours = assignment.BudgetedHours
        });
    }

    public async Task<ServiceResult> RemoveAssignmentAsync(int assignmentId)
    {
        var assignment = await _db.ProjectAssignments.FindAsync(assignmentId);
        if (assignment is null)
            return ServiceResult.Fail("Assignment not found.");

        // Note: this does not touch any Timesheets already created under this
        // assignment (Timesheet.ProjectId FK uses Restrict, not Cascade) — history is kept.
        var employeeId = assignment.EmployeeId;
        _db.ProjectAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        await SynchronizeManagerRoleAsync(employeeId);
        return ServiceResult.Ok();
    }

    public async Task<List<BudgetVsActualDto>> GetBudgetVsActualAsync(int projectId)
    {
        var assignments = await _db.ProjectAssignments
            .Include(a => a.Project)
            .Include(a => a.Employee)
            .Where(a => a.ProjectId == projectId && a.AssignmentType == AssignmentType.Consultant)
            .OrderBy(a => a.Employee.Name)
            .ToListAsync();

        if (assignments.Count == 0) return new List<BudgetVsActualDto>();

        // Rejected hours are excluded: they're hours the manager has already
        // said aren't real, so counting them against a budget would be wrong.
        var logged = await _db.TimesheetEntries
            .Where(e => e.Timesheet.ProjectId == projectId && e.Status != EntryStatus.Rejected)
            .GroupBy(e => new { e.Timesheet.EmployeeId, e.Status })
            .Select(g => new
            {
                g.Key.EmployeeId,
                g.Key.Status,
                Hours = g.Sum(e => e.HoursWorked)
            })
            .ToListAsync();

        return assignments.Select(a => new BudgetVsActualDto
        {
            ProjectId = projectId,
            ProjectName = a.Project.Name,
            ConsultantId = a.EmployeeId,
            ConsultantName = a.Employee.Name,
            BudgetedHours = a.BudgetedHours,
            ActualHours = logged.Where(h => h.EmployeeId == a.EmployeeId).Sum(h => h.Hours),
            ApprovedHours = logged
                .Where(h => h.EmployeeId == a.EmployeeId && h.Status == EntryStatus.Approved)
                .Sum(h => h.Hours)
        })
        .ToList();
    }

    private static ProjectAssignmentDto ToDto(ProjectAssignment a) => new()
    {
        Id = a.Id,
        ProjectId = a.ProjectId,
        ProjectName = a.Project.Name,
        EmployeeId = a.EmployeeId,
        EmployeeName = a.Employee.Name,
        AssignmentType = a.AssignmentType,
        BudgetedHours = a.BudgetedHours
    };

    private async Task SynchronizeManagerRoleAsync(string employeeId)
    {
        var employee = await _userManager.FindByIdAsync(employeeId);
        if (employee is null) return;

        var hasManagerAssignment = await _db.ProjectAssignments.AnyAsync(a =>
            a.EmployeeId == employeeId && a.AssignmentType == AssignmentType.Manager);
        var hasManagerRole = await _userManager.IsInRoleAsync(employee, SeedData.ManagerRole);

        if (hasManagerAssignment && !hasManagerRole)
            await _userManager.AddToRoleAsync(employee, SeedData.ManagerRole);
        else if (!hasManagerAssignment && hasManagerRole)
            await _userManager.RemoveFromRoleAsync(employee, SeedData.ManagerRole);
    }
}
